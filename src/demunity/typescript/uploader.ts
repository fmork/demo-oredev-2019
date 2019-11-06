// import { tsImportEqualsDeclaration } from "@babel/types";

enum uploaderViewstate {
    noImageSelected,
    imageSelected,
    imageUploading,
    imageProcessingFailed
}

class Uploader {



    uploadedPhotos: HTMLElement;
    imagePreview: HTMLImageElement;
    form: HTMLFormElement;
    fileElement: HTMLInputElement;
    selectImageContainer: HTMLDivElement;
    editImageContainer: HTMLDivElement;
    imageUploadingContainer: HTMLDivElement;
    assetHost: string;
    uploadIndex: number;
    apiRoot: string = '/api/photos/';
    processingFailedContainer: HTMLDivElement;

    constructor(
        assetHost: string,
        uploadedPhotos: HTMLElement) {

        this.assetHost = assetHost;
        this.uploadedPhotos = uploadedPhotos;

        this.form = document.querySelector('form') as HTMLFormElement;
        this.fileElement = this.form.querySelector('[type=file]') as HTMLInputElement;
        this.imagePreview = document.querySelector("#imagePreview") as HTMLImageElement;
        this.uploadIndex = 0;

        this.imageUploadingContainer = document.querySelector("#imageUploadingContainer") as HTMLDivElement;
        this.selectImageContainer = document.querySelector("#selectImageContainer") as HTMLDivElement;
        this.editImageContainer = document.querySelector("#editImageContainer") as HTMLDivElement;
        this.processingFailedContainer = document.querySelector("#processingFailedContainer") as HTMLDivElement;

        this.setViewState(uploaderViewstate.noImageSelected);

        if (this.fileElement != null) {
            this.fileElement.addEventListener('change', () => {
                this.previewFile(this.GetSelectedFile());
                this.setViewState(uploaderViewstate.imageSelected);
            });
        }

        // hook up form submit event handler
        if (this.form != null) {
            this.form.addEventListener('submit', e => {
                e.preventDefault();
                this.uploadFile();
            });
        }
    }

    private previewFile(file: File | null) {
        const target = this.imagePreview;
        var reader = new FileReader();

        reader.addEventListener("load", () => {
            target.src = reader.result as string;
        }, false);

        if (file) {
            reader.readAsDataURL(file);
        }
    }

    private uploadFile() {
        this.setViewState(uploaderViewstate.imageUploading);


        if (this.fileElement != null && this.fileElement.files != null) {

            const file = this.GetSelectedFile();
            if (file == null) {
                console.log("File is null");
                return;
            }

            const textElement = document.querySelector("#photoText") as HTMLTextAreaElement;
            const elementId = this.uploadIndex.toString();
            this.uploadIndex = this.uploadIndex + 1;

            this.setUploadFeedback("Uploading...");


            const headers: HeadersInit = new Headers();
            headers.set('Content-Type', 'application/json');

            const request: RequestInit = {
                method: 'POST',
                headers: headers,
                body: JSON.stringify({
                    filename: file.name,
                    text: textElement.value
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
                            this.setUploadFeedback("Wait for it...");
                            setTimeout(() => this.monitorPhotoProcess(bodyAsPhoto, elementId), 750);
                        });
                    }
                    else {
                        console.log('Could not convert "' + body + '" to a Photo');
                    }

                });
        }
    }

    private GetSelectedFile() {
        if (this.fileElement != null && this.fileElement.files != null) {
            return this.fileElement.files[0];
        }
        return null;
    }

    private monitorPhotoProcess(photo: Photo, elementId: string) {

        const uri = this.apiRoot + photo.id + '/state';
        this.uploadIndex = this.uploadIndex + 1;
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
                    window.location.assign('/photos/' + photo.id);
                }
                else {
                    if (body === 'ProcessingFailed') {
                        this.setViewState(uploaderViewstate.imageProcessingFailed);
                        return;
                    }
                    else if (body === 'ProcessingStarted') {
                        this.setUploadFeedback("Making images for the web...");
                    }
                    setTimeout(() => this.monitorPhotoProcess(photo, elementId), 750);
                }
            });
    }
    
    private setUploadFeedback(text: string) {
        const feedbackTextTarget = document.querySelector("#uploadFeedback") as HTMLSpanElement;
        if (feedbackTextTarget instanceof HTMLSpanElement) {
            feedbackTextTarget.textContent = text;
        }

    }

    private setViewState(state: uploaderViewstate) {
        switch (state) {
            case uploaderViewstate.noImageSelected:
                this.imageUploadingContainer.hidden = true;
                this.editImageContainer.hidden = true;
                this.selectImageContainer.hidden = false;
                this.processingFailedContainer.hidden = true;
                break;
            case uploaderViewstate.imageSelected:
                this.imageUploadingContainer.hidden = true;
                this.editImageContainer.hidden = false;
                this.selectImageContainer.hidden = false;
                this.processingFailedContainer.hidden = true;
                const editor = document.querySelector("#photoText");
                if (editor instanceof HTMLTextAreaElement) {
                    editor.focus();
                }
                break;
            case uploaderViewstate.imageUploading:
                this.imageUploadingContainer.hidden = false;
                this.editImageContainer.hidden = true;
                this.selectImageContainer.hidden = true;
                this.processingFailedContainer.hidden = true;
                break;
            case uploaderViewstate.imageProcessingFailed:
                this.imageUploadingContainer.hidden = true;
                this.editImageContainer.hidden = true;
                this.selectImageContainer.hidden = true;
                this.processingFailedContainer.hidden = false;
                break;
            default:
                console.log("Unknown view state: " + state)
                break;
        }
    }


}