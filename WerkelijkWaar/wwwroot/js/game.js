// Werkelijk Waar? game
// Maakt gebruik van SignalR
"use strict";

// Var
var currentUserId = 0;
var currentUserRole = 0;
var currentGameCode = "";

// SignalR test
var connection = new signalR.HubConnectionBuilder().withUrl("/gameHub").build();

//Disable send button until connection is established
document.getElementById("sendButton").disabled = true;

// SignalR
// Receive message and place it in the <ul>
connection.on("ReceiveMessage", function (userId, action, code, type) {
    if (code == currentGameCode) {
        if (type == 0) {
            // Message
            var msg = action.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
            var encodedMsg = userId + " says " + msg;
            var li = document.createElement("li");
            li.textContent = encodedMsg;
            document.getElementById("messageList").appendChild(li);
        }
        else if (type == 1) {
            // Connection test
            sendMessage(currentUserId, "Yup", currentGameCode, 0);
        }
    }
});

// Check if connected to the hub
connection.start().then(function () {
    document.getElementById("sendButton").disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});

// Send a message
document.getElementById("sendButton").addEventListener("click", function (event) {
    var userId = document.getElementById("userInput").value;
    var code = currentGameCode;
    var type = document.getElementById("typeInput").value;
    var action = document.getElementById("messageInput").value;
    connection.invoke("SendMessage", userId, action, code, type).catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});

// Send a message (function)
function sendMessage(id, action, code, type) {
    var userId = id;
    var gameCode = code;
    var msgType = type;
    var msg = action;
    connection.invoke("SendMessage", userId, msg, gameCode, msgType).catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
};