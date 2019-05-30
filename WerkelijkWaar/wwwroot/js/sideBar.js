// Switch sidebar images with its darker versions
// console.log(document.title);
var roleId = $('#hiddenRoleId').val();
console.log("Role Id: " + roleId);

// Student
if (roleId == 0) {
    if (document.title.includes("Spel -")) {
        $("#side-all-game").attr("src", "/content/image/Play.png");
        $("#side-0-scoreOverview").attr("src", "/content/image/Trophy_disabled.png");
    }
    else if (document.title.includes("Scores van")) {
        $("#side-all-game").attr("src", "/content/image/Play_disabled.png");
        $("#side-0-scoreOverview").attr("src", "/content/image/Trophy.png");
    }
    else {
        $("#side-all-game").attr("src", "/content/image/Play_disabled.png");
        $("#side-0-scoreOverview").attr("src", "/content/image/Trophy_disabled.png");
    }
}

// Teacher
if (roleId == 1) {
    if (document.title.includes("Spel -")) {
        $("#side-all-game").attr("src", "/content/image/Play.png");
        $("#side-1-gameConfig").attr("src", "/content/image/Gear_disabled.png");
        $("#side-1-classOverview").attr("src", "/content/image/People_disabled.png");
    }
    else if (document.title.includes("configuratie -")) {
        $("#side-all-game").attr("src", "/content/image/Play_disabled.png");
        $("#side-1-gameConfig").attr("src", "/content/image/Gear.png");
        $("#side-1-classOverview").attr("src", "/content/image/People_disabled.png");
    }
    else if (document.title.includes("Inzage")) {
        $("#side-all-game").attr("src", "/content/image/Play_disabled.png");
        $("#side-1-gameConfig").attr("src", "/content/image/Gear_disabled.png");
        $("#side-1-classOverview").attr("src", "/content/image/People.png");
    }
    else {
        $("#side-all-game").attr("src", "/content/image/Play_disabled.png");
        $("#side-1-gameConfig").attr("src", "/content/image/Gear_disabled.png");
        $("#side-1-classOverview").attr("src", "/content/image/People_disabled.png");
    }
}

// Admin
if (roleId == 2) {
    if (document.title.includes("Log")) {
        $("#side-23-log").attr("src", "/content/image/File.png");
        $("#side-23-users").attr("src", "/content/image/People_disabled.png");
        $("#side-2-stories").attr("src", "/content/image/Book_disabled.png");
    }
    else if (document.title.includes("Gebruikersoverzicht")) {
        $("#side-23-log").attr("src", "/content/image/File_disabled.png");
        $("#side-23-users").attr("src", "/content/image/People.png");
        $("#side-2-stories").attr("src", "/content/image/Book_disabled.png");
    }
    else if (document.title.includes("Verhalenoverzicht")) {
        $("#side-23-log").attr("src", "/content/image/File_disabled.png");
        $("#side-23-users").attr("src", "/content/image/People_disabled.png");
        $("#side-2-stories").attr("src", "/content/image/Book.png");
    }
    else {
        $("#side-23-log").attr("src", "/content/image/File_disabled.png");
        $("#side-23-users").attr("src", "/content/image/People_disabled.png");
        $("#side-2-stories").attr("src", "/content/image/Book_disabled.png");
    }
}

// SuperAdmin
if (roleId == 3) {
    if (document.title.includes("Log")) {
        $("#side-23-log").attr("src", "/content/image/File.png");
        $("#side-23-users").attr("src", "/content/image/People_disabled.png");
        $("#side-3-config").attr("src", "/content/image/Gear_disabled.png");
    }
    else if (document.title.includes("Gebruikersoverzicht")) {
        $("#side-23-log").attr("src", "/content/image/File_disabled.png");
        $("#side-23-users").attr("src", "/content/image/People.png");
        $("#side-3-config").attr("src", "/content/image/Gear_disabled.png");
    }
    else if (document.title.includes("Serverconfiguratie")) {
        $("#side-23-log").attr("src", "/content/image/File_disabled.png");
        $("#side-23-users").attr("src", "/content/image/People_disabled.png");
        $("#side-3-config").attr("src", "/content/image/Gear.png");
    }
    else {
        $("#side-23-log").attr("src", "/content/image/File_disabled.png");
        $("#side-23-users").attr("src", "/content/image/People_disabled.png");
        $("#side-3-config").attr("src", "/content/image/Gear_disabled.png");
    }
}