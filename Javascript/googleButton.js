import React from 'react';
import axios from 'axios';
import { withRouter } from 'react-router';
import { connect } from 'react-redux';
import { createUser } from '../../actions/index';
import { bindActionCreators } from 'redux';

class GoogleBtn extends React.Component{
    constructor(props){
        super(props);
    }
    
    componentDidMount() {
        this.init(); 
    }

    init = () => {
        gapi.load('auth2', () => { 
            //intializes GoogleAuth object - must be intialized before GoogleAuth methods can be used
            this.auth2 = gapi.auth2.init({
                client_id: 'CLIENT_ID',
                cookiepolicy: 'single_host_origin',
            });
        this.attachSignin(document.getElementById(this.props.id));
        });
    }
 
    attachSignin = (element) => {
        let googleUser ={};
        //attaches the id, options, and success function to the clickHandler
        this.auth2.attachClickHandler(element, {},
            (googleUser) => {
                this.onSignIn(googleUser);
            });
    }

    redirect = (user) => {
        for (let i = 0; i < user.roles.length; i++) {
            if (user.roles[i] == "Admin") {
				//admin side had different path, so router could not be used
                window.location.href = "ADMIN_DASHBOARD";
            }
        }
        if (user.roles[user.roles.length-1] !== "Admin" )
            {
                this.props.router.push('/userdashboard');
            }
    }
    
    onSignIn =(googleUser) => {
        //gets data of logged in Google user
        let profile = googleUser.getBasicProfile();
        let id_token = googleUser.getAuthResponse().id_token;
        let data = {
            tokenId: id_token
        };
        //sends the token as a request, response will be if token is valid or not
        axios.post('TOKEN_AUTH_ENDPOINT', data)
            .then(response => { 
                let currUserName = this.props.user;
                if(currUserName === null) {
                    currUserName = {
                        firstName: "",
                        middleInitial: "",
                        lastName: "",
                        dob: ""
                    }
                }
                const regModel = {
                    //Get from Redux
                    FirstName: currUserName.firstName,
                    MiddleInitial: currUserName.middleInitial,
                    LastName: currUserName.lastName,
                    DOB: currUserName.dob,
                    //Get from Google
                    Email: response.data.email,
                    ProviderId: response.data.providerUserId,
                    //Passed in by registration
                    Role: this.props.selectedRole,
                };
                //if token was valid, create a user account, log them in, and send them to the appropriate dashboard
                axios.post('THIRD_PARTY_GOOGLE_ENDPOINT', regModel)
                    .then(resp => {
                        this.props.createUser(resp.data.item);
                        this.redirect(resp.data.item);
                    })
                    .catch(err => {
                        this.props.loginError(err);
                    });
            })
            .catch(error => {
                this.props.loginError(error);
            });
    }

    render(){
        //sign in or sign up depending on if a role exists or not
        let isLogin = this.props.selectedRole === null ? 'in' : 'up';
        return(
            <div>
                <button type="button" className="btn btn-lg btn-block btn-login-g" id={this.props.id}>
                    <i className="fa fa-google-plus" aria-hidden="true"></i> {'Sign ' + isLogin + ' with Google'}
                </button>
                <hr/>
            </div>
        )
    }
}

function mapStateToProps(state){
    return { 
        user: state.user
    }
}

function mapDispatchToProps(dispatch){
    return bindActionCreators({ createUser}, dispatch)
}

export default withRouter(connect(mapStateToProps, mapDispatchToProps)(GoogleBtn));