// Kvízkérdések kérése a backendtől és a válaszok ellenőrzése.

const quizForm = document.getElementById("kvízűrlap");
const quizResults = document.getElementById("quiz-results");
const quizMessage = document.getElementById("kviz-uzenet");

function createQuizTextElement(tagName, text) {
    const element = document.createElement(tagName);
    element.textContent = text;
    return element;
}

function showQuizQuestions(questions) {
    quizResults.replaceChildren();

    const answerForm = document.createElement("form");
    answerForm.id = "kviz-valasz-urlap";

    questions.forEach(function (question, questionIndex) {
        const fieldset = document.createElement("fieldset");
        const legend = document.createElement("legend");
        legend.textContent = `${questionIndex + 1}. ${question.question}`;
        fieldset.appendChild(legend);

        question.options.forEach(function (option, optionIndex) {
            const optionWrapper = document.createElement("div");
            const input = document.createElement("input");
            const label = document.createElement("label");
            const inputId = `kerdes-${questionIndex}-valasz-${optionIndex}`;

            input.type = "radio";
            input.name = `kerdes-${questionIndex}`;
            input.id = inputId;
            input.value = optionIndex;
            input.required = true;

            label.htmlFor = inputId;
            label.textContent = option;

            optionWrapper.append(input, label);
            fieldset.appendChild(optionWrapper);
        });

        answerForm.appendChild(fieldset);
    });

    const checkButton = document.createElement("button");
    checkButton.type = "submit";
    checkButton.textContent = "Válaszok ellenőrzése";
    answerForm.appendChild(checkButton);

    answerForm.addEventListener("submit", function (event) {
        event.preventDefault();

        const formData = new FormData(answerForm);
        let correctAnswers = 0;

        const questionFieldsets = answerForm.querySelectorAll("fieldset");

        questions.forEach(function (question, questionIndex) {
            const selectedAnswer = Number(formData.get(`kerdes-${questionIndex}`));
            const correctIndex = Number(question.correctAnswerIndex);
            const isCorrect = selectedAnswer === correctIndex;

            if (isCorrect) {
                correctAnswers++;
            }

            const feedback = createQuizTextElement(
                "p",
                isCorrect
                    ? "Helyes válasz!"
                    : `Nem helyes. A helyes válasz: ${question.options[correctIndex]}`
            );
            questionFieldsets[questionIndex].appendChild(feedback);
        });

        const score = createQuizTextElement(
            "h3",
            `Eredmény: ${correctAnswers} / ${questions.length} helyes válasz`
        );
        answerForm.prepend(score);

        answerForm.querySelectorAll('input[type="radio"]').forEach(function (input) {
            input.disabled = true;
        });

        checkButton.disabled = true;
    });


    quizResults.appendChild(answerForm);
}

if (quizForm && quizResults && sessionStorage.getItem("tanuloportalFelhasznalo")) {
    quizForm.addEventListener("submit", async function (event) {
        event.preventDefault();
        hideMessage(quizMessage);

        const topic = quizForm.elements.topic.value.trim();
        const questionCount = Number(quizForm.elements.questionCount.value);
        const generateButton = quizForm.querySelector('button[type="submit"]');

        generateButton.disabled = true;
        generateButton.textContent = "Kvíz generálása...";
        quizResults.replaceChildren(createQuizTextElement("p", "A kérdések készülnek..."));

        try {
            const result = await apiRequest("/api/ai/generate-quiz", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({ topic, questionCount })
            });

            if (!result || !Array.isArray(result.questions) || result.questions.length === 0) {
                throw new Error("A szerver nem adott vissza megjeleníthető kérdéseket.");
            }

            const validQuestions = result.questions.every(function (question) {
                return question.question
                    && Array.isArray(question.options)
                    && question.options.length > 0
                    && Number.isInteger(Number(question.correctAnswerIndex))
                    && Number(question.correctAnswerIndex) >= 0
                    && Number(question.correctAnswerIndex) < question.options.length;
            });

            if (!validQuestions) {
                throw new Error("A kapott kvíz válaszai nem megfelelő formátumúak.");
            }

            showQuizQuestions(result.questions);
        } catch (error) {
            quizResults.replaceChildren(createQuizTextElement("p", "A kvíz nem készült el."));
            showMessage(quizMessage, error.message, "error");
        } finally {
            generateButton.disabled = false;
            generateButton.textContent = "Kvíz generálása";
        }
    });
}
