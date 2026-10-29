# Room design

The normal run has seven rooms. `-Design` starts in Geryon with the shotgun equipped.

| Room | Encounter | Layout and supplies |
| --- | --- | --- |
| Room01 | Safe start | Practise movement, aiming and shooting. |
| Ford | Three Chaser waves | One narrow bridge crosses the blood river. |
| Island | Three Chaser waves | Both bridges flood during the ambush. Clearing the room spawns HP on the east bank at (580, 180). |
| Spillway | Staggered Chaser ambush | Crossed rivers and geysers restrict the platform exits. The centre holds the shotgun and HP at (350, 180). |
| Crossfire | 10 / 16 / 14 enemies | Shooters cover separate lanes; the last wave includes eight Flankers. HP and armor are placed on opposite sides. |
| Furnace | 14 / 14 enemies | Three platforms form a triangle. One bridge floods after a warning while the other two stay open. More supplies arrive in wave two; clearing the room drains the exit causeway. |
| Geryon | Boss, 120 HP | At half HP, the outer floor burns and HP and armor appear inside the remaining arena. |

## Boss

Geryon summons four enemies at a time, with at least six seconds between summons
in phase one and 7.5 seconds in phase two. The limit is sixteen living minions.
Five of every eight summons are Flankers; the others are Chasers, Shooters and Chargers.
The second phase has faster shots and charges. Killing Geryon removes surviving
minions, extinguishes the fire and opens the final exit.

## Weapons and HUD

Q switches between held weapons. E replaces the active weapon with one on the
ground when both slots are occupied. To keep the shotgun when picking up the RPG,
select the pistol before pressing E.

The HUD shows the active weapon's icon and name beside the hearts. A faint Q hint
appears to its right when two weapons are held. Ground weapons are displayed at
2x scale. Unowned weapons show a floating, translucent E while the player overlaps
them with a full inventory.

The RPG fires 1.8 shots/second with a two-round magazine and a 1.25-second reload.
Rockets travel at 360 pixels/second and deal eight damage to enemies, enough to
kill a full-health Charger. Player splash damage is two.

## Audio

| Room | Track | Start and loop point |
| --- | --- | --- |
| Room01 | Argent Energy | 1:10 |
| Ford | At Doom's Gate | 0:37 |
| Island | Rust, Dust & Guts | 2:24 |
| Spillway | Flesh & Metal | 2:51 |
| Crossfire | BFG Division | 1:00 |
| Furnace | Harbinger | 1:20 |
| Geryon | Rip & Tear | 0:00 |

Music starts on entry at -8 dB and fades out over 1.5 seconds after combat is
cleared. Entering the next room cancels the old fade and starts its track. The
safe starting room keeps playing until departure.

Music and SFX use separate buses with a master limiter. SFX has 24 playback voices
and limits repeated impacts and enemy sounds. The SFX bus is at 0 dB; pistol shots
use 0 dB, shotgun and RPG shots use +2 dB, and reload/switch sounds use -5 dB.
Quieter effects have individual levels in SoundEffects.cs. Sources, processing
notes and licenses are in assets/audio/.
