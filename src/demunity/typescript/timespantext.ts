class TimeSpanText {
	constructor() {

	}

	asUtc(time: Date)
	{
		return new Date(
			time.getUTCFullYear(),
			time.getUTCMonth(),
			time.getUTCDay(),
			time.getUTCHours(),
			time.getUTCMinutes(),
			time.getUTCSeconds(),
			time.getUTCMilliseconds()
		);
	}

	getTimeString(time: Date) {

		const timeNow = new Date();
		const now = timeNow.valueOf();
		const then = time.valueOf(); // already in UTC

		// now - then gives diff in milliseconds
		const timespanInSeconds = (now - then) / 1000;

		if (timespanInSeconds < 60) {
			return timespanInSeconds <= 1.5 ? "one second ago" : Math.round(timespanInSeconds) + " seconds ago";
		}
		if (timespanInSeconds < 120) {
			return "a minute ago";
		}
		if (timespanInSeconds < 2700) // 45 * 60
		{
			return Math.round(timespanInSeconds / 60) + " minutes ago";
		}
		if (timespanInSeconds < 5400) // 90 * 60
		{
			return "an hour ago";
		}
		if (timespanInSeconds < 86400) // 24 * 60 * 60
		{
			return Math.round(timespanInSeconds / 60 / 60) + " hours ago";
		}
		if (timespanInSeconds < 172800) // 48 * 60 * 60
		{
			return "yesterday";
		}
		if (timespanInSeconds < 2592000) // 30 * 24 * 60 * 60
		{
			return Math.round(timespanInSeconds / 60 / 60 / 24) + " days ago";
		}
		if (timespanInSeconds < 31104000) // 12 * 30 * 24 * 60 * 60
		{
			const months = Math.round(Math.floor(timespanInSeconds / 60 / 60 / 24 / 30));
			return months <= 1 ? "one month ago" : months + " months ago";
		}

		const years = Math.round(Math.floor(timespanInSeconds / 60 / 60 / 24 / 30 / 365))
		return years <= 1 ? "one year ago" : years + " years ago";
	}
}