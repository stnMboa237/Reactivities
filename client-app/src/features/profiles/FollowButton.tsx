import { observer } from "mobx-react-lite"
import { Button, Reveal } from "semantic-ui-react"
import { Profile } from "../../app/models/profile"
import { useStore } from "../../app/stores/store"

interface Props {
    profile: Profile
}

export default observer(function FollowButton({ profile }: Props) {
    const { profileStore, userStore } = useStore();
    const { updateFollowing, loading } = profileStore;

    if (userStore.user?.username === profile.username) return null; /*we don't want to see the btn follow/unfollow on our own profile */

    // function handleFollow(e: HTMLButtonElement, username: string) {
    //     e.preventDefault();
    //     profile.following ? updateFollowing(username, false) : updateFollowing(username, true);
    // }

    return (
        <Reveal animated="move">
            <Reveal.Content visible style={{ width: '100%' }}>
                <Button
                    fluid
                    color='teal'
                    content={profile.following ? 'Following' : 'Not following'}
                />
            </Reveal.Content>
            <Reveal.Content hidden style={{ width: '100%' }}>
                <Button
                    fluid
                    basic
                    color={profile.following ? 'red' : 'green'}
                    content={profile.following ? 'Unfollow' : 'Follow'}
                    loading={loading}
                    // onClick={(e) => handleFollow(e, profile.username)}
                    onClick={(e) => {
                        e.preventDefault();
                        profile.following ? updateFollowing(profile.username, false) : updateFollowing(profile.username, true);
                    }}
                />
            </Reveal.Content>
        </Reveal>
    )
})