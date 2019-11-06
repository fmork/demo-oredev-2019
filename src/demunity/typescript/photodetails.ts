
class PhotoDetails {
    photo: PhotoDetail;
    userIsAuthenticated: boolean;
    userId: string;
    likeState: boolean;
    apiRoot: string = '/api/photos/';
    inEditMode: boolean;
    timeSpanText: TimeSpanText;
    httpRequestBuilder: HttpRequestBuilder;
    commentTextArea: HTMLTextAreaElement;
    addCommentButton: HTMLAnchorElement;
    addCommentEventHandler: EventListener;

    constructor(photo: PhotoDetail) {
        this.inEditMode = false;
        this.userId = photo.CurrentUserId;
        this.userIsAuthenticated = photo.CurrentUserId != null;
        this.photo = photo;
        this.likeState = photo.LikedByCurrentUser;

        this.commentTextArea = document.querySelector("#photoComment") as HTMLTextAreaElement;
        this.addCommentButton = document.querySelector("#addCommentLink") as HTMLAnchorElement;
        this.addCommentEventHandler = this.addCommentHandler.bind(this);
        this.timeSpanText = new TimeSpanText();
        this.httpRequestBuilder = new HttpRequestBuilder();
        this.setupEditControls();
        this.loadComments();
    }


    private setupEditControls() {
        if (this.userIsAuthenticated) {

            const showTextEditorLink = document.querySelector("#showTextEditorLink") as HTMLAnchorElement;
            if (showTextEditorLink instanceof HTMLAnchorElement) {
                showTextEditorLink.addEventListener("click", () => {
                    this.toggleEditmode();
                });
            }

            this.setupAddCommentButton();

            const textEditControlsElement = document.querySelector('#textEditControls');
            if (textEditControlsElement instanceof HTMLDivElement) {

                const saveTextLink = document.querySelector("#photoTextSaveLink") as HTMLAnchorElement;
                if (saveTextLink instanceof HTMLAnchorElement) {
                    saveTextLink.addEventListener("click", this.saveText.bind(this));
                }

                const cancelEditLink = document.querySelector("#cancelEditLink") as HTMLAnchorElement;
                if (cancelEditLink instanceof HTMLAnchorElement) {
                    cancelEditLink.addEventListener("click", this.cancelTextEdit.bind(this));
                }

                const photoDeleteLink = document.querySelector("#photoDeleteLink") as HTMLAnchorElement;
                if (photoDeleteLink instanceof HTMLAnchorElement) {
                    photoDeleteLink.addEventListener("click", this.deleteHandler.bind(this));
                }

                // textEditControlsElement.appendChild(this.makeButton(null, 'Save changes', this.saveText, "fas fa-save", "photo-edit-btn"));
                // textEditControlsElement.appendChild(this.makeButton(null, 'Undo edits', this.cancelTextEdit, "fas fa-undo", "photo-edit-btn"));
                // textEditControlsElement.appendChild(this.makeButton(null, 'Delete this photo (with all likes and comments)', this.deleteHandler, "fas fa-trash", "photo-edit-btn"));
            }
        }
    }

    private toggleEditmode() {
        const displayContainer = this.getTextDisplayContainer() as HTMLDivElement;
        const editContainer = this.getTextEditContainer() as HTMLDivElement;
        if (displayContainer instanceof HTMLDivElement && editContainer instanceof HTMLDivElement) {
            if (this.inEditMode) {
                editContainer.classList.add("hidden");
                displayContainer.classList.remove("hidden");
            }
            else {
                displayContainer.classList.add("hidden");
                editContainer.classList.remove("hidden");
            }
            this.inEditMode = !this.inEditMode;
        }
    }

    private setupAddCommentButton() {
        this.commentTextArea.oninput = this.setCommentButtonState.bind(this);
    }


    private setCommentButtonState() {
        const hasText = this.commentTextArea.value.length != 0;
        const linkIsDisabled = this.addCommentButton.classList.contains("disabled");
        if (hasText && linkIsDisabled) {
            this.enableCommentButton();
        }
        else if (!hasText && !linkIsDisabled) {
            this.disableCommentButton();
        }
    }

    private disableCommentButton() {
        this.addCommentButton.classList.add("disabled");
        this.addCommentButton.removeEventListener("click", this.addCommentEventHandler);
        this.addCommentButton.removeAttribute("href");
    }

    private enableCommentButton() {
        this.addCommentButton.classList.remove("disabled");
        this.addCommentButton.addEventListener("click", this.addCommentEventHandler);
        this.addCommentButton.href = "javascript:void()";
    }

    private addCommentHandler() {
        this.disableCommentButton();

        const textArea = document.querySelector("#photoComment");
        if (textArea instanceof HTMLTextAreaElement) {
            const commentText = textArea.value;
            const uri = this.apiRoot + this.photo.Id + '/comment';

            const request = this.httpRequestBuilder.getRequest('POST', JSON.stringify(commentText));

            fetch(uri, request)
                .then(response => response.json())
                .then(body => {
                    if (body === 'OK') {
                        this.loadComments();
                        textArea.value = "";
                        this.setCommentButtonState();
                    }
                });
        }
    }


    private loadComments() {
        const uri = this.apiRoot + this.photo.Id + '/comment';
        const request = this.httpRequestBuilder.getRequest('GET', null);

        fetch(uri, request)
            .then(response => response.json())
            .then(body => {
                this.displayComments(body);
            });
    }


    private displayComments(comments: Array<PhotoComment>) {
        const commentsContainer = document.querySelector("#commentsContainer");
        if (commentsContainer instanceof HTMLDivElement) {
            removeChildren(commentsContainer);
            comments.forEach(comment => {

                const commentDiv = this.makeCommentDiv(comment);

                commentsContainer.appendChild(commentDiv);
            });
        }
    }


    private makeCommentDiv(comment: PhotoComment) {
        const cmnt = new PhotoCommentObject(comment.photoId, comment.userId, comment.userName, comment.time, comment.text);
        const commentDiv = createDiv("photo-comment");
        commentDiv.appendChild(this.makeUserNameDiv(cmnt));
        commentDiv.appendChild(makeTextDiv(cmnt.text, "comment-text", null));
        const commentFooterDiv = createDiv("comment-footer smaller");
        commentFooterDiv.appendChild(makeTextDiv(this.timeSpanText.getTimeString(cmnt.time), "comment-time toned-down", cmnt.time.toLocaleString()));
        if (this.userIsAuthenticated) {
            commentFooterDiv.appendChild(this.makeCommentControlsDiv(cmnt));
        }
        commentDiv.appendChild(commentFooterDiv);
        return commentDiv;
    }

    private makeCommentControlsDiv(comment: PhotoCommentObject): any {
        const capturedComment = comment;

        const div = document.createElement("div");
        div.className = "comment-controls";
        if (this.userIsAuthenticated) {
            if (comment.userId == this.userId) {
                div.appendChild(createJsLink("delete", () => {
                    this.deleteComment(capturedComment);
                }));
            }
            else {
                // div.appendChild(createJsLink("report", () => {
                //     this.reportComment(capturedComment);
                // }));
            }
        }

        return div;
    }



    // private reportComment(comment: PhotoCommentObject) {
    //     console.log("Reporting comment '" + comment.text + "'");
    //     // to be implemented
    // }

    private makeUserNameDiv(comment: PhotoCommentObject) {
        const div = document.createElement("div");
        div.className = "smaller";
        const userLink = document.createElement("a");
        userLink.text = comment.userName;
        userLink.href = '/users/' + comment.userId;
        div.appendChild(userLink);



        return div;
    }


    private deleteComment(comment: PhotoCommentObject) {
        const uri = this.apiRoot + this.photo.Id + '/comment';
        const request = this.httpRequestBuilder.getRequest('DELETE', JSON.stringify(comment));

        fetch(uri, request)
            .then(response => response.json())
            .then(() => {
                this.loadComments();
            });
    }






    private setPhotoText(plainText: string, htmlText: string) {
        const editorTextArea = this.getPhotoTextEditElement();
        const textIsEmpty = (plainText == null || plainText == "");

        if (editorTextArea instanceof HTMLTextAreaElement) {
            editorTextArea.value = plainText;
        }

        const textDisplayDiv = this.getPhotoTextDisplayElement();
        const textEmptyDisplayDiv = this.getEmptyPhotoTextDisplayElement();
        if (textDisplayDiv instanceof HTMLElement && textEmptyDisplayDiv instanceof HTMLElement) {
            textDisplayDiv.innerHTML = htmlText;
            if (textIsEmpty) {
                textDisplayDiv.classList.add("hidden");
                textEmptyDisplayDiv.classList.remove("hidden");
            }
            else {
                textDisplayDiv.classList.remove("hidden");
                textEmptyDisplayDiv.classList.add("hidden");
            }
        }
    }


    private getPhotoTextDisplayElement() {
        return document.querySelector('#photoText');
    }

    private getEmptyPhotoTextDisplayElement() {
        return document.querySelector('#photoText-empty');
    }


    private getPhotoTextEditElement() {
        return document.querySelector('#photoTextEditor');
    }

    private getTextDisplayContainer() {
        return document.querySelector("#textDisplayContainer");
    }
    private getTextEditContainer() {
        return document.querySelector("#textEditContainer");
    }

    private cancelTextEdit() {
        this.setPhotoText(this.photo.Text, this.photo.HtmlText);
        this.toggleEditmode();
    }


    private saveText() {
        const textArea = this.getPhotoTextEditElement();
        if (textArea instanceof HTMLTextAreaElement) {
            const newPlainText = textArea.value;
            const uri = this.apiRoot + this.photo.Id + '/text';
            const requestInit = this.httpRequestBuilder.getRequest('PUT', JSON.stringify(newPlainText));

            fetch(uri, requestInit)
                .then(response => response.json())
                .then(htmlText => {

                    this.photo.Text = htmlText;
                    this.setPhotoText(newPlainText, htmlText);
                    this.toggleEditmode();
                });
        }
    }


    private async deleteHandler() {
        //     const deleteElements = document.querySelectorAll(".delete-photo-cmd");
        //     deleteElements.forEach(element => {
        //         element.setAttribute('disabled', 'true');
        //     });


        return this.showDialog(
            /*"Delete photo?", " */
            "Do you want to delete this photo (along with all likes and comments)?",
            /*"Yes", 
            "No", */
            async () => {
                const uri = this.apiRoot + this.photo.Id;
                const requestInit = this.httpRequestBuilder.getRequest('DELETE', null);
                await fetch(uri, requestInit);
                return window.location.assign('/');
            });
    }

    private showDialog(/*title: string,*/ body: string, /*okText: string, cancelText: string,*/ actionCallback: () => Promise<void>) {


        if (window.confirm(body)) {
            actionCallback();
        }

        // // set dialog heading
        // const titleDiv = document.querySelector("#dialog-title") as HTMLDivElement;
        // if (titleDiv instanceof HTMLDivElement) {
        //     this.removeChildren(titleDiv);
        //     const titleHeading = document.createElement("h5");
        //     titleHeading.append(title);
        //     titleDiv.appendChild(titleHeading);
        // }

        // // set dialog body
        // const bodyDiv = document.querySelector("#dialog-title") as HTMLDivElement;
        // if (bodyDiv instanceof HTMLDivElement) {
        //     this.removeChildren(bodyDiv);
        //     const bodyElement = document.createElement("p");
        //     bodyElement.append(body);
        //     bodyDiv.appendChild(bodyElement);
        // }

        // // setup ok button
        // const okButton = document.querySelector("#dialog-ok-button") as HTMLButtonElement;
        // if (okButton instanceof HTMLButtonElement) {
        //     this.removeChildren(okButton);
        //     okButton.append(okText);

        //     const clickHandler = () => {
        //         okButton.removeEventListener("click", clickHandler, true);
        //         actionCallback();
        //     };

        //     okButton.onclick = clickHandler;
        // }

        // // setup cancel button
        // const cancelButton = document.querySelector("#dialog-ok-button") as HTMLButtonElement;
        // if (cancelButton instanceof HTMLButtonElement) {
        //     this.removeChildren(cancelButton);
        //     okButton.append(cancelText);
        // }


        // const dialog = document.querySelector("#photo-detail-dialog") ;
        // dialog.modal(options)
    }



}
