import { createBrowserRouter, Navigate, RouteObject } from "react-router-dom";
import ActivityDashboard from "../../features/activities/dashboard/ActivityDashboard";
import ActivityDetails from "../../features/activities/details/ActivityDetails";
import ActivityForm from "../../features/activities/form/activityForm";
import NotFound from "../../features/errors/NotFound";
import ServerError from "../../features/errors/ServerError";
import TestErrors from "../../features/errors/TestError";
import ProfilePage from "../../features/profiles/ProfilePage";
import LoginForm from "../../features/users/LoginForm";
import App from "../layout/App";
import RequiredAuth from "./RequiredAuth";

export const routes: RouteObject[] = [
    {
        path: '/',
        element: <App />,
        children: [ /* element est appelé est parent de toutes nos routes. ainsi, 
        si l'user tape sur la barre d'adresse une route, 
        alors il y aura toujours un check pour verif que l'user est authentifié, 
        sinon, il sera renvoyé vers la page d'accueil */
            {element: <RequiredAuth/ >, children: [
                {path: 'activities', element: <ActivityDashboard />},
                {path: 'activities/:id', element: <ActivityDetails />},
                {path: 'createActivity', element: <ActivityForm key='create'/>}, /*unsing Key inside the same component allows react to reset the state when the component Key changed*/
                {path: 'manage/:id', element: <ActivityForm key='manage'/>},
                {path: 'profiles/:username', element: <ProfilePage/>},
                {path: 'login', element: <LoginForm/>},
                {path: 'errors', element: <TestErrors />}
            ]},
            {path: 'not-found', element: <NotFound />},
            {path: 'server-error', element: <ServerError />},
            {path: '*', element: <Navigate replace to='/not-found' />},
        ]
    }
]

export const router = createBrowserRouter(routes);