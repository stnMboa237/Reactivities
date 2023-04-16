import { useEffect, useState } from "react";
import { useStore } from "../../app/stores/store";
import useQuery from "../../app/util/hooks";
import agent from "../../app/api/agent";
import { toast } from "react-toastify";
import { Button, Header, Icon, Segment } from "semantic-ui-react";
import LoginForm from "./LoginForm";

export default function ConfirmEmail() {
    const {modalStore} = useStore();
    const email = useQuery().get("email") as string;
    const token = useQuery().get("token") as string;

    const Status = {
        Verifying: 'Verifiying',
        Failed: 'Failed',
        Success: 'Success'
    };

    const [status, setStatus] = useState(Status.Verifying);

    function handleConfirmEmailResend() {
        agent.Account.resendEmailConfirmed(email).then(() => {
            toast.success("Verification email resent - please check your email or junk email");
        }).catch(error => console.log(error));
    };

    useEffect(() => {
        agent.Account.verifyEmail(token, email).then(() => {
            setStatus(Status.Success);
        }).catch(() => {
            setStatus(Status.Failed);
        });
    }, [Status.Failed, Status.Success, token, email]);

    function getBody() {
        switch(status) {
            case Status.Verifying: 
                return <p>Verifying...</p>;
            case Status.Failed:
                return (
                    <div>
                        <p>Vreiication failed. You can try resending the verify link to your email</p>
                        <Button 
                            primary 
                            onClick={handleConfirmEmailResend} 
                            size="huge" 
                            content='Resend confirmation email'>
                        </Button>
                    </div>
                );
            case Status.Success:
                return (
                    <div>
                        <p>Email has been verified - You can now login</p>
                        <Button 
                            primary
                            onClick={() => modalStore.openModal(<LoginForm />)} 
                            size="huge"
                            content='Login'
                        />
                    </div>
                );
        }
    }

    return (
        <Segment placeholder textAlign="center">
            <Header>
                <Icon name='envelope' />
            </Header>
            <Segment.Inline>
                {getBody()}
            </Segment.Inline>
        </Segment>
    );
}