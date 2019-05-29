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

    if (document.title.includes("Score -")) {
        $("#side-all-game").attr("src", "/content/image/Play_disabled.png");
        $("#side-0-scoreOverview").attr("src", "/content/image/Trophy.png");
    }
}

// Teacher
if (roleId == 1) {
    if (document.title.includes("Spel -")) {
        $("#side-all-game").attr("src", "/content/image/Play.png");
        $("#side-1-gameConfig").attr("src", "/content/image/Gear_disabled.png");
        $("#side-1-classOverview").attr("src", "/content/image/People_disabled.png");
    }

    if (document.title.includes("configuratie -")) {
        $("#side-all-game").attr("src", "/content/image/Play_disabled.png");
        $("#side-1-gameConfig").attr("src", "/content/image/Gear.png");
        $("#side-1-classOverview").attr("src", "/content/image/People_disabled.png");
    }

    if (document.title.includes("Inzage")) {
        $("#side-all-game").attr("src", "/content/image/Play_disabled.png");
        $("#side-1-gameConfig").attr("src", "/content/image/Gear_disabled.png");
        $("#side-1-classOverview").attr("src", "/content/image/People.png");
    }
}

// Admin

// SuperAdmin