import { observer } from 'mobx-react-lite';
import { useEffect, useState } from 'react';
import InfiniteScroll from 'react-infinite-scroller';
import { Grid, Loader } from 'semantic-ui-react';
import { PagingParams } from '../../../app/models/pagination';
import { useStore } from '../../../app/stores/store';
import ActivityFilters from './ActivityFilters';
import ActivityList from './ActivityList';
import ActivityListItemPlaceholder from './ActivityListItemPlaceholder';

/*observer needs a function as parameter */
export default observer(function ActivityDashboard() {

    const { activityStore } = useStore();
    const { loadActivities, activityRegistry, setPagingParams, pagination } = activityStore;
    const [loadingNext, setLoadingNext] = useState(false);

    function handleGetNext() {
        setLoadingNext(true);
        setPagingParams(new PagingParams(pagination!.currentPage + 1));
        loadActivities().then(() => setLoadingNext(false));
    }

    // Ici, useEffect nous permet de faire des actions quand notre appli se lance.
    // En l'occurence, on appelle la methode getActivities du back
    useEffect(() => {
        /* */
        if (activityRegistry.size <= 1) {
            loadActivities();
        }
    }, [activityRegistry.size, loadActivities]); // [] permet d'executer la 'get' juste une fois. sinon, elle serait lancer n fois.

    return (
        <Grid>
            <Grid.Column width='10'>
                {activityStore.loadingInitial && !loadingNext ? (
                        <>
                            <ActivityListItemPlaceholder></ActivityListItemPlaceholder>
                        </>
                    ) : (
                        <InfiniteScroll
                            pageStart={0}
                            loadMore={handleGetNext}
                            /* hasMore is a flag used to stop calls to the API when we have already loaded all activities items 
                                !!pagination check if pagination (object) !== null 
                            */
                            hasMore={!loadingNext && !!pagination && pagination.currentPage < pagination.totalPages}
                            initialLoad={false}
                        >
                            <ActivityList />

                        </InfiniteScroll>
                    )

                }
            </Grid.Column>
            <Grid.Column width='6'>
                <ActivityFilters />
            </Grid.Column>
            <Grid.Column width={10}>
                <Loader active={loadingNext} />
            </Grid.Column>
        </Grid>
    )
})