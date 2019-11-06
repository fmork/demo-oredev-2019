class UserProfileData {
    httpRequestBuilder: HttpRequestBuilder;
    apiRoot: string = '/api/users/';

    constructor(httpRequestBuilder: HttpRequestBuilder) {
        this.httpRequestBuilder = httpRequestBuilder;
    }
    async addOnlineProfile(profile: OnlineProfile): Promise<Array<OnlineProfile>> {
        const body = JSON.stringify(profile);

        const request = this.httpRequestBuilder.getRequest('PUT', body);
        const uri = this.apiRoot + "onlineprofile";
        const response = await fetch(uri, request);
        const profiles: Array<OnlineProfile> = await response.json();
        return this.sortProfiles(profiles);
    }

    async getOnlineProfiles(userId: string): Promise<Array<OnlineProfile>> {
        const request = this.httpRequestBuilder.getRequest('GET', null);
        const uri = this.apiRoot + userId + "/onlineprofiles";
        const response = await fetch(uri, request);
        const profiles: Array<OnlineProfile> = await response.json();
        return this.sortProfiles(profiles);
    }

    async deleteOnlineProfile(profile: OnlineProfile): Promise<Array<OnlineProfile>> {
        const body = JSON.stringify(profile);

        const request = this.httpRequestBuilder.getRequest('DELETE', body);
        const uri = this.apiRoot + "onlineprofile";
        const response = await fetch(uri, request);
        const profiles: Array<OnlineProfile> = await response.json();
        return this.sortProfiles(profiles);
    }

    private sortProfiles(profiles: Array<OnlineProfile>): Array<OnlineProfile> {
        return profiles.sort((x, y) => {
            if (x.type > y.type) {
                return 1;
            }

            if (x.type < y.type) {
                return -1
            }

            return 0;
        });
    }
}