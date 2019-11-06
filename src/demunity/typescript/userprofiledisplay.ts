

class UserProfileDisplay {
    onlineProfilesContainer: HTMLDivElement;
    userId: string;
    userProfileData: UserProfileData;
    setOnlineProfilesCallback: (profiles: OnlineProfile[]) => void;
    constructor(userId: string, setOnlineProfilesCallback: (profiles: OnlineProfile[]) => void) {
        this.setOnlineProfilesCallback = setOnlineProfilesCallback != null
            ? setOnlineProfilesCallback
            : this.setOnlineProfiles.bind(this);
        this.onlineProfilesContainer = document.querySelector("#onlineProfilesContainer") as HTMLDivElement;
        this.userId = userId;
        this.userProfileData = new UserProfileData(new HttpRequestBuilder());
        this.loadOnlineProfiles();
    }

    async loadOnlineProfiles() {
        const profiles = await this.userProfileData.getOnlineProfiles(this.userId);
        this.setOnlineProfilesCallback(profiles);
    }


    setOnlineProfiles(profiles: OnlineProfile[]) {
        removeChildren(this.onlineProfilesContainer);
        if (profiles.length > 0) {
            profiles.forEach(profile => {

                const profileElement = this.getOnlineProfileElement(profile);
                const outerElement = createDiv("col-sm");
                outerElement.appendChild(profileElement);
                this.onlineProfilesContainer.appendChild(outerElement);
            });
        }
        else {
            this.onlineProfilesContainer.appendChild(this.getNoOnlineProfilesElement());
        }

    }
    getNoOnlineProfilesElement() {
        const div = createDiv("col-md toned-down");
        div.append("(no online profiles)");
        return div;
    }

    getOnlineProfileElement(profile: OnlineProfile): HTMLDivElement {
        const container = createDiv("online-profile");
        const anchor = document.createElement("a");
        anchor.href = this.getUriForOnlineProfile(profile);
        anchor.appendChild(this.getIconForOnlineProfile(profile));
        anchor.append(this.getProfileDisplayText(profile));
        container.appendChild(anchor);
        return container;
    }

    private getProfileDisplayText(profile: OnlineProfile): string {
        const profileType = profile.type.toLowerCase();
        if (profileType === "instagram" || profileType === "twitter") {
            return "@" + profile.profile;
        }
        else if (profileType === "web") {
            return profile.profile
                .replace("https://", "")
                .replace("http://", "");
        }

        return profile.profile;
    }

    private getIconForOnlineProfile(profile: OnlineProfile): HTMLElement {
        const profileType = profile.type.toLowerCase();
        const icon = document.createElement("i");
        icon.classList.add("online-profile-icon");
        if (profileType === "instagram") {
            icon.classList.add("fab");
            icon.classList.add("fa-instagram");
        }
        else if (profileType === "twitter") {
            icon.classList.add("fab");
            icon.classList.add("fa-twitter-square");
        }
        else {
            icon.classList.add("fas");
            icon.classList.add("fa-globe");
        }

        return icon;
    }

    private getUriForOnlineProfile(profile: OnlineProfile): string {
        const profileType = profile.type.toLowerCase();

        if (profileType === "instagram") {
            return "https://instagram.com/" + profile.profile;
        }

        if (profileType === "twitter") {
            return "https://twitter.com/" + profile.profile;
        }

        return profile.profile;
    }
}