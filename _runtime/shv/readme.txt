; THIS ARCHIVE REDISTRIBUTION IS NOT ALLOWED, USE THE FOLLOWING LINK INSTEAD
; http://www.dev-c.com/gtav/scripthookv/


							SCRIPT HOOK V	

v3889.0/1158.13
							
Description:
Script hook is the library that allows to use GTA V script native
functions in custom *.asi plugins.

You are allowed to use this software only if you agree to the terms
written below:
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR
THE USE OR OTHER DEALINGS IN THE SOFTWARE

Multiplayer:
Script hook closes the game when player goes Online, this is done because
the game reports installed mods list to R* while being in Online mode.

Installation:
1. Copy ScriptHookV.dll to the game's main folder, i.e. where GTA5.exe
   is located.
2. In order to load asi plugins you need to have asi loader installed,
   you can download it separately or use the latest version that comes
   with this distrib (dinput8.dll).
   You must delete old dsound.dll asiloader if you have one installed.
3. This distrib also includes a sample asi plugin - native trainer,
   if you need a trainer then copy NativeTrainer.asi as well.
4. See "how_to_install_2025.txt" for more information.
   
Changelog:
v3889.0/1158.13
- added support of the latest patches
v3788.0/1013.34
- added support of the latest patch
v3788.0/1013.33
- added support of the latest patches
v3751.0/1013.29
- added support of the latest patches
v3725.0/1013.20
- control natives compatibility
v3717.0/1013.17
- added support of the latest patches
v3586.0/889.22
- added support of the latest patch
v3586.0/889.19
- added support of the latest patches
v3570.0/889.15
- added support of the latest patches
v3521.0/814.9
- added support of the latest patches
v3504.0/813.11
- added support of the latest patches
v3442.0/812.8
- added support of the enhanced version
v1.0.3442.0
- added support of the latest patch
v1.0.3411.0
- added support of the latest patch
v1.0.3407.0
- added support of the latest patch
v1.0.3351.0
- added support of the latest patch
v1.0.3337.0
- added ability to create custom ui
- added support of the latest patch
v1.0.3323.0
- added support of the latest patch
v1.0.3274.0
- added support of the latest patch
v1.0.3258.0
- added support of the latest patch
v1.0.3179.0
- added support of the latest patch
v1.0.3095.0
- added support of the latest patch
v1.0.3028.0
- added support of the latest patch
v1.0.2944.0
- added support of the latest patch
v1.0.2845.0
- added support of the latest patch
v1.0.2824.0
- added support of the latest patch
v1.0.2802.0
- added support of the latest patch
v1.0.2699.16
- added support of the latest patch
v1.0.2699.0
- added support of the latest patch
v1.0.2628.2
- added support of the latest patch
v1.0.2612.1
- added support of the latest patch
v1.0.2545.0
- added support of the latest patch
v1.0.2372.0
- added support of the latest patch
v1.0.2245.0
- added support of the latest patch
v1.0.2215.0
- added support of the latest patch
v1.0.2189.0
- added support of the latest patch
v1.0.2060.1
- added support of the latest patch
v1.0.2060.0
- added support of the latest patch
v1.0.1868.4
- added support of the egs version
v1.0.1868.1
- added support of the latest patch
v1.0.1868.0
- added support of the latest patch
v1.0.1737.6
- added support of the latest patch
v1.0.1737.0
- added support of the latest patch
v1.0.1604.1
- added support of the latest patch
v1.0.1604.0
- added support of the latest patch
v1.0.1493.1
- added support of the latest patch
v1.0.1493.0
- added support of the latest patch
v1.0.1365.1
- added support of the latest patch
v1.0.1290.1
- added support of the latest patch
v1.0.1180.2
- added support of the latest patch
v1.0.1103.2
- added support of the latest patch
v1.0.1032.1
- added support of the latest patch
v1.0.1011.1
- added support of the latest patch
v1.0.944.2
- added support of the latest patch
v1.0.877.1
- added support of the latest patch
v1.0.791.2
- added support of the latest patch
v1.0.757.4
- added support of the latest patch
v1.0.757.2
- added support of the latest patch
v1.0.678.1
- added support of the latest patch
v1.0.617.1a
- added ability to access pickup pool
- added ability to get base object pointer using script handle
v1.0.617.1
- added support of the latest patch
v1.0.573.1a
- fixed an issue with object spawning limit with the latest patch
v1.0.573.1
- added support of the latest patch
v1.0.505.2a
- fixed an issue with starting more than 20 scripts with the latest patch
v1.0.505.2
- added support of the latest patch
v1.0.463.1
- added support of the latest patch
v1.0.393.4a
- added ability to create more objects
- added ability to access entity pools
v1.0.393.4
- added support of the latest patch
v1.0.393.2
- added support of the latest patch
v1.0.372.2a
- added directx hook
- added ability to access script globals
v1.0.372.2
- added support of the latest patch
v1.0.350.2b
- fixed thread start issue while using more than 20 scripts
   
Supported game versions:
1.0.335.2, 1.0.350.1/2, 1.0.372.2, 1.0.393.2/4, 1.0.463.1,
1.0.505.2, 1.0.573.1, 1.0.617.1, 1.0.678.1, 1.0.757.2/4,
1.0.791.2, 1.0.877.1, 1.0.944.2, 1.0.1011.1, 1.0.1032.1,
1.0.1103.2, 1.0.1180.2, 1.0.1290.1, 1.0.1365.1, 1.0.1493.0/1,
1.0.1604.0/1, 1.0.1737.0/6, 1.0.1868.0/1/4, 1.0.2060.0/1,
1.0.2189.0, 1.0.2215.0, 1.0.2245.0, 1.0.2372.0, 1.0.2545.0,
1.0.2612.1, 1.0.2628.2, 1.0.2699.0/16, 1.0.2802.0, 1.0.2824.0,
1.0.2845.0, 1.0.2944.0, 1.0.3028.0, 1.0.3095.0, 1.0.3179.0,
1.0.3258.0, 1.0.3274.0, 1.0.3323.0, 1.0.3337.0, 1.0.3351.0,
1.0.3407.0, 1.0.3411.0, 1.0.3442.0, 1.0.3504.0, 1.0.3521.0,
1.0.3570.0, 1.0.3586.0, 1.0.3717.0, 1.0.3751.0, 1.0.3788.0,
1.0.3889.0

Supported enhanced game versions:
1.0.811.8, 1.0.812.8, 1.0.813.11, 1.0.814.9, 1.0.889.15/19/22,
1.0.1013.17/29/33/34, 1.0.1158.13
   
							NATIVE TRAINER
			
Description:			
The trainer comes with ScriptHookV distrib and shows an example of what
can be done using scripts. There are two versions of the trainer in here,
one is new and is all about custom ui and the other is kept as a legacy,
while both have the same functionality speaking of features.

Changelog:
v1.0.1011.1
- added new dlc vehicles
v1.0.944.2
- added new dlc vehicles
v1.0.877.1
- added new dlc vehicles and weapons
v1.0.791.2
- added new dlc vehicles
v1.0.757.2
- added new dlc vehicles
v1.0.678.1
- added new dlc vehicles and weapons
v1.0.617.1
- added new dlc vehicles
v1.0.573.1
- added new dlc vehicles, peds and weapons
v1.0.505.2
- added new dlc vehicles, peds and weapons
v1.0.393.2
- added new dlc vehicles and weapons
v1.0.372.2a
- added strong wheels vehicle option
v1.0.372.2
- added new dlc cars and weapon
v1.0.350.2b
- added car seat belt option
- added force weather set option
- corrected car boost behaviour on car stuck
v1.0.350.2
- broken models are removed from skin changer
- fixed infinite loading on death/arrest while being in another skin
- fixed an issue when trainer was closing after showing up

Controls: 
F4					activate
NUM2/8/4/6			navigate thru the menus and lists (numlock must be on)
NUM5 				select
NUM0/BACKSPACE/F4 	back
NUM9/3 				use vehicle boost when active
NUM+ 				use vehicle rockets when active

- player
     - skin changer -- select any char you want, including animals
     - teleport -- teleport anywhere, has location set and ability
                   to perform proper map marker teleportation
     - invincible
     - fix player
     - add cash
     - wanted up/down/never
     - police ignored
     - unlimited ability
     - noiseless
     - fast swim/run
     - super jump
 - vehicle
     - speed boost -- use NUM9/3
     - car spawner
	 - strong wheels
	 - seat belt	 
     - wrap in spawned
     - paint random
     - fix
     - invincible
 - weapon
     - get all weapon
     - vehicle rockets -- use NUM+ in vehicle
     - no reload
     - fire ammo
     - explosive ammo
     - explosive melee
 - world
     - toggle random trains/boats/etc
 - time
     - hour forward/backward
     - pause
     - sync with system
 - weather
     - set wind
     - select weather
 - misc
     - change radio track
     - hide hud

Usefull links:
http://dev-c.com
http://gtaforums.com/topic/932648-script-hook-v/

							(C) Alexander Blade
								15 Jul 2026