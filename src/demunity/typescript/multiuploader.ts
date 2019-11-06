// import { tsImportEqualsDeclaration } from "@babel/types";

class MultiUploader {
    uploadedPhotos: HTMLElement;
    imagePreview: HTMLImageElement;
    form: HTMLFormElement;
    fileElement: HTMLInputElement;
    selectImageContainer: HTMLDivElement;
    editImageContainer: HTMLDivElement;
    imageUploadingContainer: HTMLDivElement;
    assetHost: string;
    apiRoot: string = '/api/photos/';

    constructor(
        assetHost: string,
        uploadedPhotos: HTMLElement) {

        this.assetHost = assetHost;
        this.uploadedPhotos = uploadedPhotos;

        this.form = document.querySelector('form') as HTMLFormElement;
        this.fileElement = this.form.querySelector('[type=file]') as HTMLInputElement;
        this.imagePreview = document.querySelector("#imagePreview") as HTMLImageElement;

        this.imageUploadingContainer = document.querySelector("#imageUploadingContainer") as HTMLDivElement;
        this.selectImageContainer = document.querySelector("#selectImageContainer") as HTMLDivElement;
        this.editImageContainer = document.querySelector("#editImageContainer") as HTMLDivElement;

        this.setViewState("no-image-selected");

        // hook up form submit event handler
        if (this.form != null) {
            this.form.addEventListener('submit', e => {
                e.preventDefault();
                this.uploadFiles();
            });
        }
    }

    private uploadFiles() {

        // this.form.hidden = true;
        // this.uploadedPhotos.hidden = false;

        this.setViewState("image-uploading");



        if (this.fileElement != null && this.fileElement.files != null) {

            for (let i = 0; i < this.fileElement.files.length; i++) {
                const file = this.fileElement.files[i];
                const thisIndex = i;

                if (file == null) {
                    console.log("File is null");
                    return;
                }

                this.setUploadFeedback(thisIndex, "Uploading...");


                const headers: HeadersInit = new Headers();
                headers.set('Content-Type', 'application/json');

                const request: RequestInit = {
                    method: 'POST',
                    headers: headers,
                    body: JSON.stringify({
                        filename: file.name,
                        text: file.name + "#hash1 #hash2 #hash3"
                    }),
                    credentials: 'include'
                };

                fetch(this.apiRoot, request)
                    .then(response => response.json())
                    .then(body => {
                        const bodyAsPhoto = body as Photo;
                        if (bodyAsPhoto != null) {
                            const uploadUrl = bodyAsPhoto.uploadUri;

                            const formData = new FormData();
                            formData.append('files[]', file);

                            const requestHeaders: HeadersInit = new Headers();
                            requestHeaders.set('Content-Type', 'multipart/form-data');
                            requestHeaders.set('Content-Length', file.size.toString());

                            const uploadRequest: RequestInit = {
                                method: 'PUT',
                                body: formData,
                                headers: requestHeaders,
                                credentials: 'include'
                            };

                            fetch(uploadUrl, uploadRequest).then(() => {
                                this.setUploadFeedback(thisIndex, "Wait for it...");
                                this.monitorPhotoProcess(bodyAsPhoto, thisIndex);
                            });
                        }
                        else {
                            console.log('Could not convert "' + body + '" to a Photo');
                        }

                    });
            }
        }
    }

    private monitorPhotoProcess(photo: Photo, fileIndex: number) {

        const uri = this.apiRoot + photo.id + '/state';

        const headers: HeadersInit = new Headers();
        headers.set('Content-Type', 'application/json');

        const requestInit: RequestInit = {
            method: 'GET',
            headers: headers,
            credentials: 'include'
        };

        fetch(uri, requestInit)
            .then(response => response.json())
            .then(body => {
                if (body === 'PhotoAvailable') {
                    this.setUploadFeedback(fileIndex, "Done");

                }
                else {
                    if (body === 'ProcessingStarted') {
                        this.setUploadFeedback(fileIndex, "Making images for the web...");
                    }
                    setTimeout(() => this.monitorPhotoProcess(photo, fileIndex), 1500);
                }
            });
    }

    private setUploadFeedback(fileIndex: number, text: string) {
        const elementId = "uploadFeedback-" + fileIndex;
        let feedbackTextTarget = document.querySelector("#" + elementId) as HTMLSpanElement;
        if (!(feedbackTextTarget instanceof HTMLSpanElement)) {


            const feedbackContainer = document.createElement("div");
            feedbackContainer.id = "uploadFeedbackContainer-" + fileIndex;
            feedbackContainer.className = "col-md-6";

            const icon = document.createElement("i");
            icon.className = "fa fa-spinner fa-spin";

            feedbackContainer.appendChild(icon);

            const span = document.createElement("span");
            span.id = "uploadFeedback-" + fileIndex;

            feedbackContainer.appendChild(span);
            feedbackTextTarget = span;
            const container = document.querySelector("#imageUploadingContainer");
            if (container instanceof HTMLDivElement) {
                container.appendChild(feedbackContainer);
            }
        }

        if (feedbackTextTarget instanceof HTMLSpanElement) {
            feedbackTextTarget.textContent = text;
        }

    }

    private setViewState(state: string) {
        switch (state) {
            case "no-image-selected":
                this.imageUploadingContainer.hidden = true;
                this.editImageContainer.hidden = false;
                this.selectImageContainer.hidden = false;
                break;
            case "image-selected":
                this.imageUploadingContainer.hidden = true;
                this.editImageContainer.hidden = false;
                this.selectImageContainer.hidden = false;

                const editor = document.querySelector("#photoText");
                if (editor instanceof HTMLTextAreaElement) {
                    editor.focus();
                }
                break;
            case "image-uploading":
                this.imageUploadingContainer.hidden = false;
                this.editImageContainer.hidden = true;
                this.selectImageContainer.hidden = true;
                break;
            default:
                console.log("Unknown view state: " + state)
                break;
        }
    }


}