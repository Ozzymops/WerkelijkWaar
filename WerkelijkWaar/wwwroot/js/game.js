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
connection.on("ReceiveMessage", function (userId, action) {
    var msg = action.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
    var encodedMsg = userId + " says " + msg;
    var li = document.createElement("li");
    li.textContent = encodedMsg;
    document.getElementById("messageList").appendChild(li);
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
    var action = document.getElementById("messageInput").value;
    connection.invoke("SendMessage", userId, action).catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});