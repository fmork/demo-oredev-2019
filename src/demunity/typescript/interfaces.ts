interface PhotoUris {
	thumbImage: string;
	fullImage: string;
}

interface Photo {
	id: string;
	uris: PhotoUris;
	userId: string;
	state: string;
	uploadUri: string;
}

interface PhotoDetail {
	Id: string;
	LikeCount: number;
	UserId: string;
	CurrentUserId: string;
	LikedByCurrentUser: boolean;
	PhotoIsOwnedByCurrentUser: boolean;
	State: string;
	Text: string;
	HtmlText: string;
}

interface PhotoComment {
	photoId: string;
	userId: string;
	userName: string;
	time: string;
	text: string;
}


interface OnlineProfile {
    type: string;
    profile: string;
}