<div align="center">

# Chino Kafuu Discord Bot - README

[![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=.net&logoColor=white)](https://dotnet.microsoft.com/)
[![Python](https://img.shields.io/badge/Python-3776AB?style=for-the-badge&logo=python&logoColor=white)](https://www.python.org/)
</div>

## ✨ **Introduction**

Welcome to Chino Kafuu, the Discord bot I've crafted using the .NET9 . As this is my first attempt at creating a bot, there might be a few errors along the way. Don't hesitate to raise a issue if you encounter any problems. I'll do my best to solve them.

---

## 🔧 **Requirements**
- Make sure you have the following installed:
- **dotnet = 9.0 (Required)** 
- **python >= 3.9 (Required)**

---

## 🔥 **New update**
- Update applio to newest version
- Improve and optimized token in/output
- Better history chat storage

## ⚠️ **Warning**
- Today (10-05-2025) the voice chat stuck at this line
```                        
connection = await channel.ConnectAsync();
```
So i can't test the audio voice. Lets wait DSharpPlus fix this and then i will try to test and upgrade Applio to the newest version
---


## 🚀 Get started
To get started, you must download the release file then follow my instructions:
- Download newest release file
- Add your token to `._env`. After done that please rename the file to '.env'

---

## 🧑‍💻 **How to use?**
- Install all python package and config the file like i said before
- Use cmd and cd to Applio folder and run TTSApi.py
- For example:
```bash
cd .\Applio
.\env\python.exe .\TTSApi.py
```
- Run c# console app
```bash
cd .\ChinoKafuu 
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

- And more... You can try / in discord to find out.

---

## 🌟 **Upcoming features**
- 🛠️ Improve and retructor code
- Create one-click install
- Paging for anilist data searching

---

## 📚 **Library used**
- [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus)
- [OsuNet](https://github.com/Blackcat76iT/OsuNet/tree/29571b5270b52c628a809225ce32c20573b65a3b) 
- [generativeAI](https://github.com/google/generative-ai-docs)
- [Applio](https://github.com/IAHispano/Applio)
- [cs-anilist](https://github.com/nupniichan/cs-anilist)
- [cs-owm](https://github.com/nupniichan/cs-owm)
- [csteam-works](https://github.com/nupniichan/csteam-works)
---

## 📝 **License**

This project is licensed under the MIT License. See the [LICENSE](https://github.com/nupniichan/ChinoKafuu/blob/main/LICENSE) file for more details.  

---

## 💡 **Contributing**
Any contributions are highly appreciated! Feel free to submit issues or pull requests. Let's make Chino Kafu even better together! 🤝

---

<div align="center">Thank you for visiting this respository! More features are coming soon ❤️.</div>
<div align="center">My angel Chino Kafuu (つ≧▽≦)つ.</div>
