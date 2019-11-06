
function removeChildren(container: HTMLElement) {
    let lastChild = container.lastElementChild;
    while (lastChild) {
        container.removeChild(lastChild);
        lastChild = container.lastElementChild;
    }
}

function makeTextDiv(text: string, cssClass: string, title: string | any) {
    const div = document.createElement("div");
    div.className = cssClass;
    if (title) {
        div.title = title;
    }
    div.append(text);
    return div;
}
function createDiv(className: string) {
    const div = document.createElement("div");
    div.className = className;
    return div;
}
function createJsLink(text: string, clickHandler: () => void) {
    const link = document.createElement("a");
    if (text.length > 0)
    {
        link.text = text;
    }
    link.href = "javascript:void(0)";
    link.onclick = clickHandler;
    return link;
}