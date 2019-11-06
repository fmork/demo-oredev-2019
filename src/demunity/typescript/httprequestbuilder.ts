class HttpRequestBuilder {

    getRequest(method: string, body: string | any) {
        const headers: HeadersInit = new Headers();
        headers.set('Content-Type', 'application/json');
        const request: RequestInit = {
            method: method,
            headers: headers,
            credentials: 'include'
        };

        if (body) {
            request.body = body;
        }

        return request;
    }
}