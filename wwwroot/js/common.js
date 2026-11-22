async function apiRequest(url, options = {}) {
    const response = await fetch(url, options);
    const responseText = await response.text();

    let responseData = null;

    if (responseText) {
        try {
            responseData = JSON.parse(responseText);
        } catch {
            responseData = responseText;
        }
    }

    if (!response.ok) {
        let errorMessage = "Hiba történt a kérés feldolgozásakor.";

        if (typeof responseData === "string" && responseData.trim()) {
            errorMessage = responseData;
        } else if (responseData?.message) {
            errorMessage = responseData.message;
        } else if (responseData?.title) {
            errorMessage = responseData.title;
        }

        throw new Error(errorMessage);
    }

    return responseData;
}

function showMessage(element, message, type = "info") {
    if (!element) {
        return;
    }

    element.textContent = message;
    element.className = `uzenet uzenet-${type}`;
    element.hidden = false;
}

function hideMessage(element) {
    if (!element) {
        return;
    }

    element.textContent = "";
    element.hidden = true;
}

window.apiRequest = apiRequest;
window.showMessage = showMessage;
window.hideMessage = hideMessage;
