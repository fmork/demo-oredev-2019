class LazyImageLoader {
    observer: IntersectionObserver;


    constructor() {
        const options: IntersectionObserverInit = {
            root: null,
            rootMargin: '0px 0px 200px 0px'
        };

        this.observer = new IntersectionObserver(this.onIntersection.bind(this), options);

        const lazyImgs = document.querySelectorAll('.lazy-img');
        lazyImgs.forEach(img => this.observer.observe(img));
    }

    onIntersection(images: IntersectionObserverEntry[]) {
        images.forEach(img => {
            if (img.isIntersecting) {
                this.observer.unobserve(img.target);

                const typedImg: HTMLImageElement = img.target as HTMLImageElement;

                // if the dataset source is not set, nothing to do
                if (typedImg.dataset.src === undefined) {
                    return;
                }

                const src = (typedImg.dataset.src as string);
                const srcSet = (typedImg.dataset.srcset as string);
                const sizes = (typedImg.dataset.sizes as string);

                // empty the dataset props to save some space
                typedImg.removeAttribute('data-sizes');
                typedImg.removeAttribute('data-srcset');
                typedImg.removeAttribute('data-src');

                typedImg.src = src;
                typedImg.srcset = srcSet;
                typedImg.sizes = sizes;

            }
        });
    }
}