# Chino Kafuu: A cute waitress girl for your discord server

**Welcome to Chino Kafu, your very own anime-loving Discord companion!**  Built with .NET 7.0, Chino offers a range of features from Osu! stats to Anilist searches, voice channel management, and even engaging conversations with Chino herself!

## ✨ Features ✨

* **Osu! Enthusiast:**  Get detailed Osu! information using the `/ohelp` command.
* **Anilist Explorer:**  Search for anime and manga directly (anilist) in Discord with `/anilhelp`.
* **Voice Channel Wizard:** Easily create voice channels with a click! Chino will even move you to the new channel. (Customization available in `Program.CS` - VoiceChannelHandler)
* **Chat with Chino-Chan:** Converse with Chino in a dedicated channel!  She'll even join your voice channel and respond using Japanese voice synthesis!  (Response times may vary depending on hardware).
* **Music Maestro:** Chino can play music in your voice channel!  Use `/start <url or song name>` or `/musicHelp` for more commands.

## 🚀 Getting Started

**Prerequisites:**

* **.NET 7.0** (Required)
* **Java 17** (Required)
* **Python 3.9** (Required)
* **Config the file** (Required)

**Installation:**

1. **First Time Setup:**
   * Run `run-install.bat` inside the `Applio-3.2.0` folder.
   * Configure `ChinoKafuRelease\ChinoKafuu\Configs\config.json` with your bot token and settings.
   * Open `ChinoKafuu.csproj` in your IDE and build the project.
   * Run `normalrun.bat` located in `ChinoKafuRelease\ChinoKafuu\bin\Debug\net7.0\`.
   * Follow Applio's web interface instructions to download Chino's voice model (https://huggingface.co/Timur04129/Chino-Kafuu/resolve/main/chino-kafuu.zip)). 

2. **Subsequent Runs:**
   * Simply use `normalrun.bat` to start Chino.

## 💡 What's New

* **Enhanced Response Times:**  Enjoy snappier replies from Chino!
* **Chat History:** Chino remembers previous conversations (up to 50 messages by default). Adjust `MAX_CHAT_HISTORY_LENGTH` in `Engine/Gemini/Gemini.py` for customization.

## 🛠️ Libraries Used

* [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus)
* [OsuNet](https://github.com/Blackcat76iT/OsuNet/tree/29571b5270b52c628a809225ce32c20573b65a3b)
* [Anilist-GraphQL](https://github.com/AniList/ApiV2-GraphQL-Docs)
* [PythonNet](https://github.com/pythonnet/pythonnet)
* [generativeAI](https://github.com/google/generative-ai-docs)
* [LavaLink4Net](https://github.com/angelobreuer/Lavalink4NET)
* [Applio](https://github.com/IAHispano/Applio)
* [FFmpeg](https://github.com/FFmpeg/FFmpeg)

## 🙌 Support
This bot is currently on progressing, but with your help, we can make it even more awesome! I welcome contributions of all kinds - bug reports, feature suggestions, code improvements, you name it! <br>
Interested in lending a hand?  Feel free to open an issue or submit a pull request. Let's build Chino Kafuu together!
