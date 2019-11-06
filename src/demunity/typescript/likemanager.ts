class LikeManager {
    apiRoot: string = '/api/photos/';
    httpRequestBuilder: HttpRequestBuilder;

    constructor() {
        this.httpRequestBuilder = new HttpRequestBuilder();
        this.initialize();
    }

    private initialize() {
        document.querySelectorAll(".likeIcon")
            .forEach(likeIcon => {
                const typedAnchor = likeIcon as HTMLAnchorElement;
                if (typedAnchor instanceof HTMLAnchorElement) {
                    typedAnchor.addEventListener("click", this.toggleLikeState.bind(this));
                }
            });
    }

    private toggleLikeState(e: MouseEvent): void {
        // temporarily detach event handler, to prevent double clicks

        var typedAnchor = e.currentTarget as HTMLAnchorElement;
        typedAnchor.removeEventListener("click", this.toggleLikeState);
        const icon = typedAnchor.querySelector("i") as HTMLElement;
        icon.className = "fas fa-hourglass-half";
        if (typedAnchor instanceof HTMLAnchorElement) {
            const likeState = typedAnchor.getAttribute("data-likestate");
            const photoId = typedAnchor.getAttribute("data-photoid") as string;
            const currentState = likeState === 'true';
            this.doLikeUnlike(photoId, !currentState)
                .then(newState => {
                    const delta = newState ? 1 : -1;

                    typedAnchor.setAttribute("data-likestate", newState.toString().toLowerCase());

                    // update icon
                    icon.className = (newState ? "fas" : "far") + " fa-star";

                    // update counter
                    const parent = typedAnchor.parentElement as HTMLElement;
                    if (parent instanceof HTMLElement) {
                        const dataNumber = parent.querySelector(".data-nbr") as HTMLSpanElement;
                        if (dataNumber instanceof HTMLSpanElement) {
                            let currentNumber = parseInt(dataNumber.textContent as string);
                            currentNumber = currentNumber + delta;
                            dataNumber.textContent = currentNumber.toString();

                        }
                    }

                    typedAnchor.addEventListener("click", this.toggleLikeState);

                });
        }
    }

    private doLikeUnlike(photoId: string, newState: boolean) {


        const cmd = newState ? 'like' : 'unlike';
        const uri = this.apiRoot + photoId + '/' + cmd;
        const requestInit = this.httpRequestBuilder.getRequest('PUT', null);

        return fetch(uri, requestInit)
            .then(response => response.json())
            .then(body => {
                return body === 'OK' ? newState : !newState;
            });
    }


}