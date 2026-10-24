# Project instructions

## Authorization before edits

- Do not modify project files until the user explicitly asks for the relevant changes. Questions, requests for explanations, reviews, and design discussions authorize reading and advice, not implementation.
- A request to create or edit a document authorizes that document only; it does not authorize changing scripts, scenes, prefabs, assets, packages, or project settings.
- Before making authorized changes, explain the implementation plan in 3–6 bullets.
- Wait for approval before major architectural changes. If the user's implementation request already explicitly approves the described architecture, proceed within that scope.
- Keep edits limited to the requested task. Do not perform unsolicited cleanup, refactoring, dependency upgrades, scene migrations, or menu changes.
- Read-only inspection is allowed when needed to answer the user's question. If verification would modify files or alter the open Unity session beyond the authorized scope, explain the proposed verification and ask first.

## Project context

- This repository is the shared Unity game-jam project: `Game-Jam-2026-2027`.
- Unity version: **2021.3.11f1**. Check `ProjectSettings/ProjectVersion.txt` before relying on version-specific APIs.
- Gameplay uses top-down 2D physics: `Rigidbody2D`, `Collider2D`, and 2D collision/trigger callbacks.
- The movement controller uses the built-in `UnityEngine.Input` API with separate keyboard controls per player.
- Preserve existing work and uncommitted changes. Do not revert or overwrite other contributors' changes.

## Current scenes and gameplay

- `Assets/Scenes/Menu_scene.unity`: menu scene.
- `Assets/Scenes/Game.unity`: scene currently loaded by `Main_Menu.Play()`.
- `Assets/Scenes/RealGame.unity`: imported top-down gameplay prototype.
- Do not assume the menu already loads RealGame. Inspect the menu script and Build Settings when menu integration is requested.
- Yellow `Player` uses **WASD**; magenta `Player2` uses **arrow keys**.
- Both players use `TopDownPlayerMovement`, with separate serialized control schemes, normalized diagonal movement, acceleration, and deceleration.
- Cyan `SecondCharacter` is a stationary touch target, separate from Player2. Either playable character can make it disappear.
- `Assets/Prefabs/Player.prefab` and `Assets/Prefabs/Player2.prefab` are the playable character prefabs.
- See `LOCAL_MULTIPLAYER.md` for the local multiplayer setup.

## Unity editing practices

- Preserve asset GUIDs and `.meta` files. Include corresponding metadata when adding or moving assets.
- Avoid editing generated folders such as `Library`, `Temp`, `Logs`, and `obj`.
- Make lasting scene and Inspector edits outside Play mode.
- Do not change packages, input settings, or render-pipeline settings unless required by the authorized task.
- Inspect current files before editing: this context may change as contributors work.

## Verification and reporting

- Use checks appropriate to the requested change. Do not claim physical keyboard testing when checks only drive movement programmatically.
- An explicit local multiplayer smoke check is available under **Game Jam > Check Local Two Players (while playing)**. Inspect its assumptions before running it; it changes temporary play state and stops Play mode afterwards.
- Check relevant Unity compilation and runtime errors. Distinguish existing errors from errors introduced by the change.
- Report what changed, what was verified, and any remaining limitations. If nothing was changed, state that clearly when relevant.
