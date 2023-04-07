import { makeAutoObservable, reaction, runInAction } from "mobx";
import agent from "../api/agent";
import { Activity, ActivityFormValues } from "../models/activity";
import { Pagination, PagingParams } from "../models/pagination";
import { Profile } from "../models/profile";
import { store } from "./store";

export default class ActivityStore {

    activityRegistry = new Map<string, Activity>();
    selectedActivity: Activity | undefined = undefined;
    editMode = false;
    loading = false;
    loadingInitial = false;
    pagination: Pagination | null = null;
    pagingParams = new PagingParams();
    predicate = new Map().set('all', true);

    constructor() {
        makeAutoObservable(this);
        reaction(
            () => this.predicate.keys(), /* we observe if the keys into the predicate change then a switch case will take action */
            () => {
                this.pagingParams = new PagingParams();
                this.activityRegistry.clear();
                this.loadActivities();
            }
        )
    }

    setPagingParams = (pagingParams: PagingParams) => {
        this.pagingParams = pagingParams;
    }

    setPredicate = (predicate: string, value: string | Date) => {
        const resetPredicate = () => {
            this.predicate.forEach((value, key) => {
                if(key !== 'startDate') {
                    this.predicate.delete(key);
                }
            });
        }
        switch(predicate) {
            case 'all': 
                resetPredicate();
                this.predicate.set('all', true);
                break;
            case 'isGoing': 
                resetPredicate();
                this.predicate.set('isGoing', true);
                break;
            case 'isHost':
                resetPredicate();
                this.predicate.set('isHost', true);
                break;
            case 'startDate':
                this.predicate.delete('startDate');
                this.predicate.set('startDate', value);
                break;
        }
    }

    get axiosParams () {
        const params = new URLSearchParams();
        params.append('pageNumber', this.pagingParams.pageNumber.toString());
        params.append('pageSize', this.pagingParams.pageSize.toString());

        this.predicate.forEach((value, key) => {
            if(key === 'startDate') {
                params.append(key, (value as Date).toISOString());
            }else{
                params.append(key, value);
            }
        })
        return params;
    }

    /*action: returns activities sorted by date*/
    get activitiesByDate() {
        return Array.from(this.activityRegistry.values()).sort((a, b) => a.date!.getTime() - b.date!.getTime());
    }

    /*action: returns many maps where every maps
    is  map<key:string(date), Activity[]> */
    get groupedActivities() {
        return Object.entries(
            this.activitiesByDate.reduce((activities, activity) => {
                const date = activity.date!.toISOString().split('T')[0]; //get the date of the current activity: key 
                /*then, scan the activities array. if the current act 'activity' has the same date 
                with activity from the 'activities', then add the current activity into the array
                else, create a new array and add it into */
                activities[date] = activities[date] ? [...activities[date], activity] : [activity];
                return activities;
            }, {} as { [key: string]: Activity[] })
            /*the type of the Object.entries is a map 
            having string as key and array of activities as value
            
            {} as { [key: string]: Activity[] }is the initial value (entry Array of map<string, Activity[]>)
            of the returned object*/
        );
    }

    /*exple d'arrow function asynchrone*/
    loadActivities = async () => {
        /*parlant de funct dont on wait la promise, c'est d'etre asynchrone
        le code non asynchrone DOIT se trouver hors du bloc try/catch*/
        this.setLoadingInitial(true);
        try {
            const result = await agent.Activities.list(this.axiosParams);
            result.data.forEach(activity => {
                this.setActivity(activity);
            });
            this.setPagination(result.pagination);
            this.setLoadingInitial(false);

        } catch (error) {
            console.log(error);
            this.setLoadingInitial(false);
        }
    }

    setPagination = (pagination: Pagination) => {
        this.pagination = pagination;
    }

    loadActivity = async (id: string) => {
        let activity = this.getActivity(id);
        if (activity) {
            this.selectedActivity = activity;
            return activity;
        }
        else {
            this.setLoadingInitial(true);
            try {
                activity = await agent.Activities.details(id);
                this.setActivity(activity);
                runInAction(() => {
                    this.selectedActivity = activity; /*toute modif d'une propriete observable dans une 
                    action doit se faire dans un runInAction*/
                });
                this.setLoadingInitial(false);
                return activity;
            } catch (error) {
                console.log(error);
                this.setLoadingInitial(false);
            }
        }
    }

    private setActivity = (activity: Activity) => {
        const user = store.userStore.user;
        if (user !== null || user !== undefined) {
            activity.isGoing = activity.attendees!.some(a => {
                return a.username === user?.username; /* le createur de l'activité part par defaut á son activité*/
            });
            activity.isHost = activity.hostUsername === user?.username;
            activity.host = activity.attendees?.find(x => x.username === activity.hostUsername);
        }
        activity.date = new Date(activity.date!);
        this.activityRegistry.set(activity.id, activity);
    }

    private getActivity = (id: string) => {
        return this.activityRegistry.get(id);
    }

    setLoadingInitial = (state: boolean) => {
        this.loadingInitial = state;
    }

    createActivity = async (activity: ActivityFormValues) => {
        const user = store.userStore.user;
        const profile = new Profile(user!);
        try {
            await agent.Activities.create(activity);
            const newActivity = new Activity(activity);
            newActivity.hostUsername = user!.username;
            newActivity.attendees = [profile];
            this.setActivity(newActivity);
            runInAction(() => {
                this.selectedActivity = newActivity;
            });
        } catch (error) {
            console.log(error);
        }
    }

    updateActivity = async (activity: ActivityFormValues) => {
        try {
            await agent.Activities.update(activity);
            runInAction(() => {
                if (activity.id) {
                    /*spread operator ... allows us to quickly copy all or part of an existing 
                    array or object into another array or object */
                    let updatedActivity = { ...this.getActivity(activity.id), ...activity };
                    /* update an existing activity based on key (id) / or insert a new one*/
                    this.activityRegistry.set(activity.id, updatedActivity as Activity);
                    this.selectedActivity = updatedActivity as Activity;
                }
            });
        } catch (error) {
            console.log(error);
        }
    }

    deleteActivity = async (id: string) => {
        this.loading = true;
        try {
            await agent.Activities.delete(id);
            runInAction(() => {
                this.activityRegistry.delete(id);
                this.loading = false;
            });
        } catch (error) {
            console.log(error);
            runInAction(() => {
                this.loading = false;
            });
        }
    }

    updateAttendance = async () => {
        const user = store.userStore.user;
        this.loading = true;
        try {
            await agent.Activities.attend(this.selectedActivity!.id);
            runInAction(() => {
                if (this.selectedActivity?.isGoing) {
                    /*si l'user actuelle participe deja (dans ce cas, l'user voudrait supprimer sa participation) 
                        á l'activité courante, alors, on le retire de l'array des participants */
                    this.selectedActivity.attendees = this.selectedActivity
                        .attendees?.filter(a => a.username !== user?.username);
                    this.selectedActivity.isGoing = false;
                } else {
                    /*si l'user courant ne participe pas encore á l'activité (ds ce cas, l'user click sur Join activity), 
                        alors, on cree un Profile et l'ajoute aux participants */
                    const attendee = new Profile(user!);
                    this.selectedActivity?.attendees?.push(attendee);
                    this.selectedActivity!.isGoing = true;
                }
                /*après l'ajout d'un participant á une activité, on update la liste des activités pour le front */
                this.activityRegistry.set(this.selectedActivity!.id, this.selectedActivity!);
            });
        } catch (error) {
            console.log(error);
        } finally {
            /*en casn d'erreur/succès, on arrete le loading en settant le loading flag a false */
            runInAction(() => this.loading = false);
        }
    }

    cancelActivityToggle = async () => {
        this.loading = true;
        try {
            /* updt the activity server side*/
            await agent.Activities.attend(this.selectedActivity!.id);
            runInAction(() => {
                /*and then updt it on front end side */
                this.selectedActivity!.isCancelled = !this.selectedActivity?.isCancelled;
                this.activityRegistry.set(this.selectedActivity!.id, this.selectedActivity!);
            })
        } catch (error) {
            console.log(error);
        } finally {
            runInAction(() => this.loading = false);
        }
    }

    clearSelectedActivity = () => {
        this.selectedActivity = undefined;
    }

    updateAttendeeFollowing = (username: string) => {
        this.activityRegistry.forEach(activity => {
            activity.attendees.forEach(attendee => {
                if (attendee.username === username) {
                    attendee.following ? attendee.followersCount-- : attendee.followingCount++;
                    attendee.following = !attendee.following;
                }
            })
        })
    }
}