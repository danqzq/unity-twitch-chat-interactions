# Contributing to Unity Twitch Chat Interactions

Thank you for your interest in helping improve *Unity Twitch Chat Interactions*! Contributions of all kinds are welcomed - from bug reports to feature suggestions to code improvements. To help things go smoothly, please follow this guide.

## How to Get Started

1. **Fork** the repository and clone your fork locally.

```git clone https://github.com/danqzq/unity-twitch-chat-interactions.git```

```cd unity-twitch-chat-interactions```

2. **Create a branch** for your work.

```git checkout -b feature/your-feature-name```

   Use a descriptive branch name (e.g., `bugfix/command-parsing`, `feature/enable-logging`).

3. Make your changes. This could be:
   * fixing a bug
   * improving documentation
   * adding or refining unit tests
   * adding a new feature or sample
4. **Ensure everything builds** in Unity and any relevant sample/demo scenes work correctly.
5. Commit your changes with a clear commit message.
6. Push your branch and open a **Pull Request (PR)** against the `main` branch in the upstream repo.

## Pull Request Checklist

Before submitting your PR, please make sure:

* [ ] You have synced your branch with the latest `main` so there are no merge conflicts.
* [ ] Your commit message clearly describes *what* you changed and *why*.
* [ ] The build in Unity still runs without errors and expected functionality behaves correctly.
* [ ] If applicable, added or updated tests.
* [ ] You followed the coding style used in the project (C#, Unity conventions).
* [ ] The PR description explains the motivation, what was changed, and how to test.

## Reporting Bugs or Requesting Features

If you find a bug or want to request a new feature:

* **Search existing issues** to see if it has already been reported.
* If not, open a new issue and include as much of the following as you can:

  * Unity version you are using
  * Version of this package (see Releases)
  * Steps to reproduce the bug (if applicable)
  * What you expected to happen vs what actually happened
  * Any error messages, logs or screenshots
    For feature requests: describe what you’d like, why it’s useful, and suggest how it might work if you have thoughts.

## Code Style & Guidelines

* The code is written in **C#** targeting Unity. Please follow existing conventions (naming, spacing, comments).
* Keep classes and methods focused; if you are adding large functionality consider splitting it into manageable chunks.
* Document any new public APIs with XML comments so they appear in IntelliSense.

## Testing

There are no formal automated tests at present (unless otherwise added). For any non-trivial changes you should:

* Test the change in Unity Editor (and ideally in a built player if feasible)
* Validate that existing functionality still works (e.g., chat commands trigger, any UI/hook remains functional)

## Project Roadmap & Communication

* You can check existing open issues to see what’s planned or requested already.
* If you’re working on a large feature, it’s often helpful to open a **discussion or issue** first so we can align on approach/design.
* You can reach the maintainer via GitHub issues; please be respectful and patient. Contributions are voluntary.

## License

* This project is licensed under the **MIT License**. By contributing you agree that your contributions will be licensed under the same terms.

## Thank You

Thanks again for considering contributing to Unity Twitch Chat Interactions! Every contribution, big or small, helps improve this tool for everyone. Looking forward to your input.

Happy coding,

Dan
