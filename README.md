<div align="center">

# Chino Kafu Discord Bot - README

[![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=.net&logoColor=white)](https://dotnet.microsoft.com/)
[![Python](https://img.shields.io/badge/Python-3776AB?style=for-the-badge&logo=python&logoColor=white)](https://www.python.org/)
</div>

## ✨ **Introduction**

Welcome to Chino Kafu, the Discord bot I've crafted using the dotnet framework. As this is my first attempt at creating a bot, there might be a few errors along the way. Don't hesitate to contact me if you encounter any issues. I'll do my best to resolve them.

---

## 🚀 Get started
To get started, you must download the release file then follow my instructions:
- Download newest release file
- Add your token to `_config.json`. After done that please rename the file to 'config.json'
- Use cmd ( or venv it's on you ) cd to Applio folder and use this command ```pip install -r requirement.txt``` to install package needed

---

## 🔧 **Requirements**
- Make sure you have the following installed:
- **dotnet = 9.0 (Required)** 
- **python 3.9 (Required)**

---

## 🆕**New update**
- Due to Youtube update so Lavalink is not working anymore on VPS so i decided to remove Youtube music commands
- Improve response time of chatting
- Update Applio to newest version (3.2.8) and sync to make sure that api can run core.py of Applio

---

## 🧑‍💻 **How to use?**
- Install all python package and config the file like i said before
- Use cmd and cd to Applio folder and run api.py
- For example:
```bash
cd E:\ChinoKafuu\Applio # Change to your path
uvicorn api:app --reload
```
- Run c# console app
```bash
cd E:\ChinoKafuu\ChinoKafuu # Change to your path
dotnet restore
dotnet build
dotnet run
```

---

## ⚙️ **Features**

Chino Kafu provide many of features for your Discord server:

- **Osu Commands**: Use the command `/ohelp` to access a list of osu-related commands, providing detailed information related to the Osu! game.

- **Anilist Commands**: Type `/anilhelp` to receive a list of commands related to Anilist, which offer assistance in exploring anime and manga information without using browser.

- **Automatically Create VoiceChat Channels**: With just a click, you can create a voice chat channel. Click the channel you want to create, and a new channel will appear. The bot will automatically move you to the newly created channel. You can adjust the channel's initialization in `Program.CS` (VoiceChannelHandler).

- **Conversation with Chino-Chan**: You will be able to chat with Chino-Chan. If you want to hear her voice, join any voice channel on your server first, after a few seconds, Chino will join channel and say what she reply to you ( She only reply ja-jp language only ). Note: *No prefix needed while chatting in that channel*.

---

## 🌟 **Upcoming features**
- 🎮 Get game and user information on Steam
- 🛠️ Improve and retructor code
- Improve prompt
- Remove unnecessary folder/file/code
- Create one-click install
- Create my own voice dataset and train it
- Add current datetime to make sure Chino know what time is it
- Create dockerfile

---

## 📚 **Library used**
- [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus)
- [OsuNet](https://github.com/Blackcat76iT/OsuNet/tree/29571b5270b52c628a809225ce32c20573b65a3b)
- [Anilist-GraphQL](https://github.com/AniList/ApiV2-GraphQL-Docs)
- [generativeAI](https://github.com/google/generative-ai-docs)
- [Applio](https://github.com/IAHispano/Applio)
- [SimpleAnilist](https://github.com/nupniichan/SimpleAnilist)

---

## 📝 **License**

This project is licensed under the MIT License. See the [LICENSE](https://github.com/nupniichan/ChinoKafuu/blob/main/LICENSE) file for more details.  

---

## 💡 **Contributing**
Any contributions are highly appreciated! Feel free to submit issues or pull requests. Let's make Chino Kafu even better together! 🤝

---

<div align="center">Thank you for using Chino Kafu! More features are coming soon~.</div>
