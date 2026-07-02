$(document).ready(function () {

    //=========================
    // MỞ CHATBOT
    //=========================
    $("#chatbot-icon").click(function () {
        $("#chatbot-box").addClass("show");
    });

    //=========================
    // ĐÓNG CHATBOT
    //=========================
    $(document).on("click", "#close-chat", function () {
        $("#chatbot-box").removeClass("show");
    });

    //=========================
    // GỬI BẰNG NÚT
    //=========================
    $("#send-chat").click(function () {
        sendMessage();
    });

    //=========================
    // ENTER ĐỂ GỬI
    //=========================
    $("#chat-message").keypress(function (e) {

        if (e.which == 13) {

            sendMessage();

            return false;

        }

    });

    //=========================
    // HÀM GỬI TIN NHẮN
    //=========================
    function sendMessage() {

        let message = $("#chat-message").val().trim();

        if (message === "")
            return;

        // Tin nhắn người dùng
        $("#chat-content").append(`
            <div class="user-message">
                ${escapeHtml(message)}
            </div>
        `);

        $("#chat-message").val("");

        scrollBottom();

        // Hiệu ứng AI đang trả lời
        $("#chat-content").append(`
            <div class="bot-message loading">
                <i class="fas fa-spinner fa-spin"></i>
                AI đang trả lời...
            </div>
        `);

        scrollBottom();

        $.ajax({

            url: "/ChatBot/Ask",

            type: "POST",

            data: {
                message: message
            },

            success: function (res) {

                setTimeout(function () {

                    $(".loading").remove();

                    if (res.success) {

                        let content = "";

                        if (res.isHtml) {
                            content = res.answer;
                        }
                        else {
                            content = formatMessage(res.answer);
                        }

                        $("#chat-content").append(`
        <div class="bot-message">
            ${content}
        </div>
    `);

                    }

                    scrollBottom();

                }, 500);

            },

            error: function () {

                $(".loading").remove();

                $("#chat-content").append(`
                    <div class="bot-message">
                        Không thể kết nối đến máy chủ.
                    </div>
                `);

                scrollBottom();

            }

        });

    }

    //=========================
    // CUỘN XUỐNG CUỐI
    //=========================
    function scrollBottom() {

        let body = $("#chat-content");

        body.scrollTop(body[0].scrollHeight);

    }

    //=========================
    // HIỂN THỊ XUỐNG DÒNG
    //=========================
    function formatMessage(text) {

        if (!text)
            return "";

        return escapeHtml(text)
            .replace(/\n/g, "<br>");

    }

    //=========================
    // CHỐNG HTML
    //=========================
    function escapeHtml(text) {

        return $("<div>").text(text).html();

    }

});