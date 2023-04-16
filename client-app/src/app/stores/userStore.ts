import { makeAutoObservable, runInAction } from "mobx";
import agent from "../api/agent";
import { User, UserFormValues } from "../models/user";
import { router } from "../router/Routes";
import { store } from "./store";

export default class UserStore {
    user: User | null = null;
    fbLoading = false;
    refreshTokenTimeOut : any;

    constructor(){
        makeAutoObservable(this)
    }

    get isLoggedIn (){
        return !!this.user;
    }

    refreshToken = async() => {
        try {
            const user = await agent.Account.refreshToken();
            runInAction(() => this.user = user)
            store.commonStore.setToken(user.token);
            this.startRefreshTokenTimer(user);
        } catch (error) {
            console.log(error);
        }
    }

    private startRefreshTokenTimer (user: User) { //Buffer.from(data, 'base64')
        const jwtToken = JSON.parse(atob(user.token.split('.')[1])); //atob is deprecated
        const expires = new Date(jwtToken.exp * 1000); // delai d'expiration
        const timeOut = expires.getTime() - Date.now() - (60*1000); // delai d'attente = 60secondes avant l'expiration du token
        this.refreshTokenTimeOut = setTimeout(this.refreshToken, timeOut); // this method will be transparent for the user
    }

    private stopRefreshTokenTimer () {
        clearTimeout(this.refreshTokenTimeOut)
    }

    login = async(creds: UserFormValues) => {
        try {
            const user = await agent.Account.login(creds);
            store.commonStore.setToken(user.token);
            this.startRefreshTokenTimer(user);
            runInAction(() => this.user = user);
            router.navigate('/activities');
            store.modalStore.closeModal(); //after login, we need to close the modal
        } catch (error) {
            throw error;
        }
    }

    register = async(creds: UserFormValues) => {
        try {
            await agent.Account.register(creds);
            router.navigate(`/account/registerSuccess?email=${creds.email}`);
            store.modalStore.closeModal(); //after loggin, we need to close the modal
        } catch (error: any) {
            if(error?.response?.status === 400)throw error;
            store.modalStore.closeModal();
            console.log(500);
        }
    }

    logout = () => {
        store.commonStore.setToken(null);
        this.stopRefreshTokenTimer();
        this.user = null;
        router.navigate('/'); /*back to homepage*/
    }

    getUser = async() => {
        try {
            const user = await agent.Account.current();
            store.commonStore.setToken(user.token);
            runInAction(() => this.user = user);
            this.startRefreshTokenTimer(user);
        } catch (error) {
            console.log(error);
        }
    }

    setImage = (image: string) => {
        if(this.user){
            this.user.image = image;
        }
    }

    setDisplayName(displayName: string) {
        if(this.user) {
            this.user.displayName = displayName;
        }
    }

    facebookLogin = async(accessToken: string) => {
        try {
            this.fbLoading = true;
            const user = await agent.Account.fbLogin(accessToken);
            store.commonStore.setToken(user.token);
            this.startRefreshTokenTimer(user);
            runInAction(() => {
                this.user = user;
                this.fbLoading = false;
            });
            router.navigate("/activities");
        } catch (error) {
            console.log(error);
            runInAction(() => this.fbLoading = false);
        }
    }
}