class PhotoCommentObject {
	photoId: string;
	userId: string;
	userName: string;
	time: Date;
	text: string;

	constructor(photoId: string, userId: string, userName: string, time: string, text: string) {
		this.photoId = photoId;
		this.userId = userId;
		this.userName = userName;
		this.time = new Date(Date.parse(time));
		this.text = text;
	}
}