// A feladatok betöltése, hozzáadása, módosítása és törlése.

const taskForm = document.getElementById("feladat-űrlap");
const taskList = document.getElementById("feladatlista");
const taskMessage = document.getElementById("feladat-uzenet");

const statusLabels = {
    planned: "Tervezett",
    "in-progress": "Folyamatban",
    completed: "Kész"
};

const priorityLabels = {
    Low: "Alacsony",
    Normal: "Normál",
    High: "Magas"
};

function createTextElement(tagName, text, className = "") {
    const element = document.createElement(tagName);
    element.textContent = text;

    if (className) {
        element.className = className;
    }

    return element;
}

function createSelectField(labelText, name, options, selectedValue) {
    const wrapper = document.createElement("div");
    const label = document.createElement("label");
    const select = document.createElement("select");

    label.textContent = labelText;
    select.name = name;
    select.required = true;

    options.forEach(function (option) {
        const optionElement = document.createElement("option");
        optionElement.value = option.value;
        optionElement.textContent = option.label;
        optionElement.selected = option.value === selectedValue;
        select.appendChild(optionElement);
    });

    wrapper.append(label, select);
    return { wrapper, select };
}

function createEditForm(task) {
    const form = document.createElement("form");
    form.className = "feladat-szerkeszto-urlap";

    const titleWrapper = document.createElement("div");
    const titleLabel = document.createElement("label");
    const titleInput = document.createElement("input");
    titleLabel.textContent = "Feladat címe";
    titleInput.type = "text";
    titleInput.id = `edit-title-${task.id}`;
    titleLabel.htmlFor = titleInput.id;
    titleInput.name = "title";
    titleInput.value = task.title || "";
    titleInput.required = true;
    titleWrapper.append(titleLabel, titleInput);

    const descriptionWrapper = document.createElement("div");
    const descriptionLabel = document.createElement("label");
    const descriptionInput = document.createElement("textarea");
    descriptionLabel.textContent = "Leírás";
    descriptionInput.id = `edit-description-${task.id}`;
    descriptionLabel.htmlFor = descriptionInput.id;
    descriptionInput.name = "description";
    descriptionInput.rows = 3;
    descriptionInput.value = task.description || "";
    descriptionWrapper.append(descriptionLabel, descriptionInput);

    const statusField = createSelectField("Állapot", "status", [
        { value: "planned", label: "Tervezett" },
        { value: "in-progress", label: "Folyamatban" },
        { value: "completed", label: "Kész" }
    ], task.status);

    const priorityField = createSelectField("Prioritás", "priority", [
        { value: "Low", label: "Alacsony" },
        { value: "Normal", label: "Normál" },
        { value: "High", label: "Magas" }
    ], task.priority);

    const buttonWrapper = document.createElement("div");
    const saveButton = document.createElement("button");
    const cancelButton = document.createElement("button");
    saveButton.type = "submit";
    saveButton.textContent = "Mentés";
    cancelButton.type = "button";
    cancelButton.textContent = "Mégse";
    buttonWrapper.append(saveButton, cancelButton);

    const editMessage = document.createElement("p");
    editMessage.hidden = true;

    form.append(
        titleWrapper,
        descriptionWrapper,
        statusField.wrapper,
        priorityField.wrapper,
        buttonWrapper,
        editMessage
    );

    cancelButton.addEventListener("click", function () {
        loadTasks();
    });

    form.addEventListener("submit", async function (event) {
        event.preventDefault();
        saveButton.disabled = true;

        const updatedTask = {
            id: task.id,
            title: titleInput.value.trim(),
            description: descriptionInput.value.trim(),
            status: statusField.select.value,
            priority: priorityField.select.value
        };

        try {
            await apiRequest(`/api/tasks/${task.id}`, {
                method: "PUT",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(updatedTask)
            });

            hideMessage(taskMessage);
            await loadTasks();
            showMessage(taskMessage, "A feladat módosítása sikerült.", "success");
        } catch (error) {
            showMessage(editMessage, error.message, "error");
        } finally {
            saveButton.disabled = false;
        }
    });

    return form;
}

function createTaskElement(task) {
    const listItem = document.createElement("li");
    const details = document.createElement("div");
    details.className = "feladat-adatok";

    details.appendChild(createTextElement("h4", task.title || "Névtelen feladat"));

    if (task.description) {
        details.appendChild(createTextElement("p", task.description));
    }

    const statusText = statusLabels[task.status] || task.status || "Nincs megadva";
    const priorityText = priorityLabels[task.priority] || task.priority || "Nincs megadva";
    details.appendChild(createTextElement("p", `Állapot: ${statusText}`));
    details.appendChild(createTextElement("p", `Prioritás: ${priorityText}`));

    const buttonWrapper = document.createElement("div");
    const editButton = document.createElement("button");
    const deleteButton = document.createElement("button");
    editButton.type = "button";
    editButton.textContent = "Módosítás";
    deleteButton.type = "button";
    deleteButton.textContent = "Törlés";
    buttonWrapper.append(editButton, deleteButton);
    details.appendChild(buttonWrapper);

    editButton.addEventListener("click", function () {
        listItem.replaceChildren(createEditForm(task));
    });

    deleteButton.addEventListener("click", async function () {
        const shouldDelete = window.confirm(`Biztosan törlöd ezt a feladatot: ${task.title}?`);

        if (!shouldDelete) {
            return;
        }

        deleteButton.disabled = true;

        try {
            await apiRequest(`/api/tasks/${task.id}`, {
                method: "DELETE"
            });

            await loadTasks();
            showMessage(taskMessage, "A feladat törlése sikerült.", "success");
        } catch (error) {
            showMessage(taskMessage, error.message, "error");
            deleteButton.disabled = false;
        }
    });

    listItem.appendChild(details);
    return listItem;
}

async function loadTasks() {
    taskList.replaceChildren(createTextElement("li", "Feladatok betöltése..."));
    hideMessage(taskMessage);

    try {
        const tasks = await apiRequest("/api/tasks");
        taskList.replaceChildren();

        if (!Array.isArray(tasks) || tasks.length === 0) {
            taskList.appendChild(createTextElement("li", "Még nincs feladatod. Adj hozzá egyet a fenti űrlappal!"));
            return;
        }

        tasks.forEach(function (task) {
            taskList.appendChild(createTaskElement(task));
        });
    } catch (error) {
        taskList.replaceChildren(createTextElement("li", "A feladatokat nem sikerült betölteni."));
        showMessage(taskMessage, error.message, "error");
    }
}

if (taskForm && taskList && sessionStorage.getItem("tanuloportalFelhasznalo")) {
    taskForm.addEventListener("submit", async function (event) {
        event.preventDefault();
        hideMessage(taskMessage);

        const submitButton = taskForm.querySelector('button[type="submit"]');
        const newTask = {
            title: taskForm.elements.title.value.trim(),
            description: taskForm.elements.description.value.trim(),
            status: taskForm.elements.status.value,
            priority: taskForm.elements.priority.value
        };

        submitButton.disabled = true;
        submitButton.textContent = "Hozzáadás...";

        try {
            await apiRequest("/api/tasks", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(newTask)
            });

            taskForm.reset();
            taskForm.elements.status.value = "planned";
            taskForm.elements.priority.value = "Normal";
            await loadTasks();
            showMessage(taskMessage, "A feladat hozzáadása sikerült.", "success");
        } catch (error) {
            showMessage(taskMessage, error.message, "error");
        } finally {
            submitButton.disabled = false;
            submitButton.textContent = "Feladat hozzáadása";
        }
    });

    loadTasks();
}
