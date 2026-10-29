# TDDD23 Game Project Plan: The 7th Ring

## Project members:

- Axel Glöckner, axegl999
- Olle Håkansson, ollha403

---

## Game Idea

The 7th Ring is a single-player 2D action game with a top-down view. The player fights through a series of rooms in a hellish dungeon, collects weapons and upgrades, and eventually faces a final boss.

The art style is directly inspired by the original DOOM, featuring low-resolution pixel art, industrial environments, and demonic creatures.

The gameplay focuses on fast, responsive combat that requires the player to avoid enemy attacks, discover upgrades, and make decisions under pressure. Rooms vary in size, layout, and content, including combat encounters, loot rooms, and a boss arena. Clearing a combat room opens the way forward, while dying ends the run.

Our main inspirations are The Binding of Isaac for its room-based progression and upgrades, and DOOM for its atmosphere, weapons, and aggressive combat.

We aim for each run to last approximately 10–15 minutes and consist of around 8–10 rooms. The game will include approximately 3–4 weapon types, four regular enemy types, one final boss, health and armor pickups, and a small selection of upgrades, such as increased fire rate, increased damage, and lifesteal.

The opening encounters will follow a fixed order so that mechanics can be introduced gradually as the player learns.

---

## Game Tech

We will use Godot Engine with C#, targeting a downloadable desktop game played with a mouse and keyboard.

We will use Godot’s 2D scene system for rooms, characters, weapons, and pickups, together with its collision, animation, and audio systems. C# will handle player controls, enemy behavior, combat, upgrades, and progression between rooms.

The room-based structure will allow us to reuse assets and build varied encounters from a small set of reusable components.

We will use Git for version control and GitHub to host the repository and collaborate.

---

## Progression in the Game

The game will gradually introduce new actions and mechanics through a predetermined sequence of encounters.

The opening room will provide a safe space to move and aim. The player will start with a basic pistol and can use this room to get a feel for the controls and shooting mechanics.

The first encounter will introduce a basic enemy that moves directly toward the player. Later encounters will introduce a ranged enemy with slow, clearly visible projectiles, followed by faster enemies that attempt to flank the player. Finally, tougher, tank-like enemies will introduce charging attacks.

Each enemy type will first appear alone before being combined with previously introduced enemies. Different pairings and group sizes will gradually increase the complexity of encounters. Later rooms may contain all four types, requiring the player to prioritize threats and adapt their movement.

Rooms will also vary in shape, obstacle placement, and interactive elements. For example, explosive barrels may detonate when shot or touched, creating opportunities and hazards that affect how the player approaches an encounter.

Loot rooms will provide quieter moments between fights. Upgrades such as increased fire rate or stronger knockback will allow the player to develop different strengths during a run, while health and armor pickups will provide opportunities to recover.

The final boss will combine familiar attack cues and behaviors from earlier enemies into a more demanding encounter. The boss will also spawn regular enemies, requiring the player to manage several threats at once. A possible later phase may change the boss’s attack sequence and create different openings for the player to attack, building on what they have already learned.

---

## In-game feedback and visuals for teaching the game

We will teach the mechanics through gameplay, visual feedback, and audio cues rather than written instructions.

The player’s weapon, and potentially the character model, will visibly turn toward the mouse cursor to communicate how aiming works. Shots will produce muzzle flashes and impact effects, while enemies will visibly react to hits by flashing red. Weapons such as the shotgun or bazooka may also push the player backward when fired. The first enemy will appear alone and approach slowly, giving the player time to experiment with movement and shooting.

Enemy attacks will use consistent visual and audio cues. A ranged enemy will visibly prepare to fire, while a charging enemy will pause and adopt a distinct pose before rushing forward. Faster enemies will attempt to flank the player and retreat if their attack fails. Early encounters will provide enough space to observe and learn these behaviors safely. Later encounters will reuse the same cues under greater pressure.

Weapons will have distinct firing sounds, projectile patterns, and degrees of recoil or knockback. Some weapons may also reduce movement speed while held, giving them a noticeable effect on how the player moves and fights.

Explosive barrels will be red to distinguish them from background decorations and will visibly react when hit. The first barrels will be placed in clear, open positions so that the player can experiment from a safe distance before encountering them in crowded rooms.

Pickups will use recognizable shapes and consistent effects. Collecting health will visibly increase the health indicator and trigger a healing effect on the player, accompanied by an audio cue. Armor pickups will similarly update a separate shield indicator and provide distinct visual and audio feedback.

Weapon upgrades will produce observable changes, such as faster firing or greater knockback. If time allows, upgrade choices will include small animated previews to communicate their effects.

Combat-room doors will visibly unlock when the last enemy dies, accompanied by a sound cue and changes in lighting and background music. When the player takes damage, a brief red flash will clearly communicate the hit.

We will observe new players without explaining the mechanics and check whether they understand aiming, enemy attack cues, pickups, and room completion. Moments of confusion will guide improvements to the encounters and feedback.
