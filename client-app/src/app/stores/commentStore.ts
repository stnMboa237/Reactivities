import { HubConnection } from "@microsoft/signalr";
import { HubConnectionBuilder } from "@microsoft/signalr/dist/esm/HubConnectionBuilder";
import { LogLevel } from "@microsoft/signalr/dist/esm/ILogger";
import { makeAutoObservable, runInAction } from "mobx";
import { ChatComment } from "../models/comment";
import { store } from "./store";

export default class CommentStore {
    comments: ChatComment[] = [];
    hubConnection: HubConnection | null = null;

    constructor() {
        makeAutoObservable(this);
    }

    createHubConnection = (activityId: string) => {
        if (store.activityStore.selectedActivity) {
            this.hubConnection = new HubConnectionBuilder()
                .withUrl(process.env.REACT_APP_CHAT_URL + '?activityId=' + activityId, {
                    accessTokenFactory: () => store.userStore.user?.token!
                })
                .withAutomaticReconnect()
                .configureLogging(LogLevel.Information)
                .build();

            // active la connection á l'HUB pour l'activité ayant Id activityId. Donc tous les commentaires liées á cette activités vont se charger
            this.hubConnection.start().catch(error => console.log('Error establishing the connection to the HUB: ' + error));

            // et pour charger/recup les commentaires liés á l'activityId, on use la méthode defini dans le HUB coté back onConnectedAsync. 
            // le nom de la methode á take se call "LoadComments" dans ce cas. LES NOMS DOIVENT MATCHER!!!
            this.hubConnection.on('LoadComments', (commentsFromBack: ChatComment[]) => {
                runInAction(() => {
                    commentsFromBack.forEach(com => {
                        com.createdAt = new Date(com.createdAt);
                    });
                    this.comments = commentsFromBack
                });
            });

            this.hubConnection.on('ReceiveComment', (commentToSend: ChatComment) => {
                runInAction(() => {
                    commentToSend.createdAt = new Date(commentToSend.createdAt);
                    this.comments.unshift(commentToSend); //unshfift put a new item at the top of the array
                });
            });
        }
    }

    stopHubConnection = () => {
        this.hubConnection?.stop().catch(error => console.log('Error stopping connection the the HUB: ' + error));
    }

    clearComments = () => {
        this.comments = [];
        this.stopHubConnection();
    }

    addComment = async(values: any) => {
        values.activityId = store.activityStore.selectedActivity?.id;
        try {
            /*on appelle directement la methode back SendComment qui a son tour invoquera la fonction ReceiveComment 
            pour dispatcher les nouvo comm aux users du monde */
            await this.hubConnection?.invoke('SendComment', values);
        } catch (error) {
            console.log(error);
        }
    }
}