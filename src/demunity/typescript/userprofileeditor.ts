class UserProfileEdit {
    profileNameInput: HTMLInputElement;
    addOnlineProfileLink: HTMLAnchorElement;
    selectProfileType: HTMLSelectElement;
    onlineProfilesContainer: HTMLDivElement;
    userId: string;
    userProfileData: UserProfileData;
    userProfileDisplay: UserProfileDisplay;
    constructor(userId: string) {
        this.userProfileDisplay = new UserProfileDisplay(userId, this.setOnlineProfiles.bind(this));
        this.profileNameInput = document.querySelector("#profileName") as HTMLInputElement;
        this.addOnlineProfileLink = document.querySelector("#addOnlineProfileLink") as HTMLAnchorElement;
        this.selectProfileType = document.querySelector("#onlineProfileType") as HTMLSelectElement;
        this.onlineProfilesContainer = document.querySelector("#onlineProfilesContainer") as HTMLDivElement;
        this.userId = userId;
        this.userProfileData = new UserProfileData(new HttpRequestBuilder());
        this.setupControls();
    }

    private setupControls() {
        if (this.profileNameInput != null && this.selectProfileType != null) {
            this.profileNameInput.addEventListener("input", this.onlineProfileInputHandler.bind(this));
            this.selectProfileType.addEventListener("input", this.onlineProfileInputHandler.bind(this));
        }
    }


    private onlineProfileInputHandler() {
        const canBeSaved = this.profileNameInput.value.length > 0 && this.selectProfileType.value.length > 0;
        const linkIsDisabled = this.addOnlineProfileLink.classList.contains("disabled");
        if (canBeSaved && linkIsDisabled) {
            this.enableAddOnlineProfileButton();
        }
        else if (!canBeSaved && !linkIsDisabled) {
            this.disableAddOnlineProfileButton();
        }

        this.setOnlineNamePlaceholder();
    }

    private setOnlineNamePlaceholder() {
        switch (this.selectProfileType.value.toLowerCase()) {
            case "twitter":
            case "instagram":
                this.profileNameInput.placeholder = "@name";
                break;
            case "web":
                this.profileNameInput.placeholder = "https://";
                break;
        }
    }

    private disableAddOnlineProfileButton() {
        this.addOnlineProfileLink.classList.add("disabled");
        this.addOnlineProfileLink.removeAttribute("href");
        this.addOnlineProfileLink.removeEventListener("click", this.addOnlineProfile);
    }

    private enableAddOnlineProfileButton() {
        this.addOnlineProfileLink.classList.remove("disabled");
        this.addOnlineProfileLink.href = "javascript:void(0)";
        this.addOnlineProfileLink.addEventListener("click", this.addOnlineProfile.bind(this));
    }

    private async addOnlineProfile() {
        const profile = {} as OnlineProfile;
        profile.type = this.selectProfileType.value;
        profile.profile = this.profileNameInput.value;

        this.resetAddOnlineProfileControls();
        const profiles = await this.userProfileData.addOnlineProfile(profile);

        this.setOnlineProfiles(profiles);
    }

    private setOnlineProfiles(profiles: OnlineProfile[]) {
        removeChildren(this.onlineProfilesContainer);
        profiles.forEach(profile => {
            const localProfile = profile;
        
            const containerDiv = createDiv("profile-edit-container");
            const onlineProfileDiv = this.userProfileDisplay.getOnlineProfileElement(localProfile);

            const deleteProfileDiv = createDiv("delete-profile-cmd");
            const deleteLink = createJsLink("", () => this.deleteOnlineProfile(localProfile));
            const icon = document.createElement("i");
            icon.className = "fas fa-trash-alt";
            deleteLink.appendChild(icon);
            deleteProfileDiv.appendChild(deleteLink);
            containerDiv.appendChild(onlineProfileDiv);
            containerDiv.appendChild(deleteProfileDiv);

            this.onlineProfilesContainer.appendChild(containerDiv);
        });
    }

    private resetAddOnlineProfileControls() {
        this.profileNameInput.value = "";
        this.selectProfileType.value = "";
    }
    private async deleteOnlineProfile(localProfile: OnlineProfile): Promise<void> {
        const profiles = await this.userProfileData.deleteOnlineProfile(localProfile);
        this.setOnlineProfiles(profiles);
    }
}