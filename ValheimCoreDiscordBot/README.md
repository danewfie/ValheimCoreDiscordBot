# Discord Valheim Bot
## _Experimental Discord.Net built with .Net Core_
[TOC]

#### Prerequisite 
1. Windows OS
2. steamcmd 

#### About this project
A discord bot that self hosts a Valheim Server so all users within the discord server can control the server. Allowing for real time notifications of events happening within the Valheim World.

#### Discord Commands

| Command | Description |
| ------ | ------ |
| Start | Starts Valheim Server |
| Stop | Stops Valheim Server |
| Status | Displays status about the server |
| Server | Displays server loggin information |
| Commands | List of all commands |

#### config.json
The application requires a **config.json** file to be present at the root of the application or placed in the same directory as the executible. Feel free to copy past this output into your own **config.json** file and update the values as required.

```
{
  "Token": "Discord Token",
  "Prefix": "#",
  "Channel": "Text Channel ID that you want to send ",
  "Password": "serverpassword",
  "Port": "2456",
  "ServerDir": "C:\\steamcmd\\steamapps\\common\\Valheim dedicated server",
  "ServerBat": "start_headless_server_bob.bat"
}
```

#### Installation/Setup
1. Ensure steamcmd is setup and installed
2. Install Valheim Dedicated Server
3. Create a discord app attached to your account to generate Discord Token
4. Update config.json file with appropriate values
5. Compile code
6. Run Discord Bot

#### Future Enhancements
| Enhancement | Description |
| ------ | ------ |
| Linux | Enable suppport for both windows and linux servers |
| Server Update | Ability to update Valheim Server through discord commands |
| Local Database | Integrate a local database to log persistent data for statistics tracking |

