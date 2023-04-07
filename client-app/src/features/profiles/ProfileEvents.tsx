import { observer } from 'mobx-react-lite';
import { format } from 'date-fns';
import { useEffect } from 'react';
import { Link } from 'react-router-dom';
import { Card, Grid, Header, Tab, Image } from 'semantic-ui-react'
import { UserActivity } from '../../app/models/profile';
import { useStore } from '../../app/stores/store';

const panes = [
    { menuItem: 'Future Events', pane: { key: 'future' } },
    { menuItem: 'Past Events', pane: { key: 'past' } },
    { menuItem: 'Hosting', pane: { key: 'hosting' } },
];

export default observer(function ProfileEvents() {
    const { profileStore: { loadingUserActivities, loadUserActivities, profile, userActivities } } = useStore();

    useEffect(() => {
        if (profile) {
            loadUserActivities(profile.username, panes[0].pane.key);
        }
    }, [loadUserActivities, profile]);


    return (
        <Tab.Pane loading={loadingUserActivities}>
            <Grid>
                <Grid.Column width={16}>
                    <Header
                        floated='left'
                        icon='calendar'
                        content='Activities'
                    />
                </Grid.Column>
                <Grid.Column width={16}>
                    <Tab
                        panes={panes}
                        menu={{ secondary: true, pointing: true }}
                        onTabChange={(e, data) => {
                            if (profile) {
                                loadUserActivities(profile.username, panes[data.activeIndex as number].pane.key /*the key is the predicate*/);
                            }
                        }}
                    />
                    <br />
                    <Card.Group itemsPerRow={4}>
                        {userActivities.map((userActivity: UserActivity) => (
                            <Card
                                as={Link}
                                to={`/activities/${userActivity.id}`}
                                key={userActivity.id}
                            >
                                <Image
                                    src={`/assets/categoryImages/${userActivity.category}.jpg`}
                                    style={{ minHeight: 100, objectFit: 'cover' }}
                                />
                                <Card.Content>
                                    <Card.Header textAlign='center'>{userActivity.title}</Card.Header>
                                    <Card.Meta>
                                        <div>{format(new Date(userActivity.date), 'do LLL')}</div>
                                        <div>{format(new Date(userActivity.date), 'h:mm a')}</div>
                                    </Card.Meta>
                                </Card.Content>
                            </Card>
                        ))}
                    </Card.Group>
                </Grid.Column>
            </Grid>
        </Tab.Pane>
    )
})