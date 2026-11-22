const loginForm = document.getElementById("bejelentkezes-űrlap");
const registerForm = document.getElementById("regisztrációs-űrlap");
const loginMessage = document.getElementById("bejelentkezes-uzenet");
const registerMessage = document.getElementById("regisztracio-uzenet");
const logoutLink = document.getElementById("kijelentkezes-link");

const protectedPage = document.body.dataset.requiresLogin === "true";
const savedUsername = sessionStorage.getItem("tanuloportalFelhasznalo");

if (protectedPage && !savedUsername) {
    window.location.replace("bejelentkezes.html");
}

if (loginForm) {
    loginForm.addEventListener("submit", async function (event) {
        event.preventDefault();
        hideMessage(loginMessage);

        const username = loginForm.elements.username.value.trim();
        const password = loginForm.elements.password.value;
        const submitButton = loginForm.querySelector('button[type="submit"]');

        submitButton.disabled = true;
        submitButton.textContent = "Bejelentkezés...";

        try {
            await apiRequest("/api/auth/login", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({ username, password })
            });

            sessionStorage.setItem("tanuloportalFelhasznalo", username);
            window.location.href = "fooldal.html";
        } catch (error) {
            showMessage(loginMessage, error.message, "error");
        } finally {
            submitButton.disabled = false;
            submitButton.textContent = "Bejelentkezés";
        }
    });
}

if (registerForm) {
    registerForm.addEventListener("submit", async function (event) {
        event.preventDefault();
        hideMessage(registerMessage);

        const username = registerForm.elements.username.value.trim();
        const email = registerForm.elements.email.value.trim();
        const password = registerForm.elements.password.value;
        const submitButton = registerForm.querySelector('button[type="submit"]');

        submitButton.disabled = true;
        submitButton.textContent = "Regisztráció...";

        try {
            const result = await apiRequest("/api/auth/register", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({ username, email, password })
            });

            showMessage(
                registerMessage,
                result?.message || "Sikeres regisztráció! Most már bejelentkezhetsz.",
                "success"
            );
            registerForm.reset();
        } catch (error) {
            showMessage(registerMessage, error.message, "error");
        } finally {
            submitButton.disabled = false;
            submitButton.textContent = "Regisztráció";
        }
    });
}

if (logoutLink) {
    logoutLink.addEventListener("click", function () {
        sessionStorage.removeItem("tanuloportalFelhasznalo");
    });
}
