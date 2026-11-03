<!-- Keep in sync with AGENTS.md at the repository root. -->

# Agent instructions: Aspiring .NET with OpenAI and Ollama

This repository holds the slides and demos for Chris Ayers' talk **Aspiring .NET with Azure OpenAI and Ollama (also presented as Building Smarter .NET Apps with Aspire, Azure OpenAI, and Ollama)**. The deck is published to <https://chris-ayers.com/build-with-aspire/>.

## Repository layout

- `slides/`: Marp deck, images in `slides/img/`, themes in `slides/themes/`.
- `BuildWithAspire.slnx`: the demo solution. `src/BuildWithAspire.AppHost/` orchestrates the `ApiService`, `Web`, `MCPServer`, and shared `ServiceDefaults`/`Abstractions` projects.
- `tests/`: unit tests for the API, AppHost, and web projects (see `tests/README.md`).
- `api-tests/`: `.http` files for exercising the running services; `docs/demo-guide.md` is the live-demo script.

## Slides

- Write the deck in Markdown with [Marp](https://marp.app/). `slides/Slides.md` is the source; `build/` is generated output and is never edited or committed.
- Keep the frontmatter at the top of the deck:

  ```yaml
  ---
  marp: true
  theme: custom-default
  ---
  ```

- Store themes in `slides/themes/` and images and diagram sources in `slides/img/`. Prefer theme classes and CSS variables over inline styles.
- Write speaker notes as HTML comments (`<!-- -->`).
- Render locally with the Marp CLI version the Pages workflow uses (latest):

  ```bash
  npx @marp-team/marp-cli --theme-set slides/themes --html -o build/index.html -- slides/Slides.md
  npx @marp-team/marp-cli --theme-set slides/themes --html --pdf --allow-local-files -o build/Slides.pdf -- slides/Slides.md
  ```

- `.github/workflows/marp-pages.yml` builds the HTML deck and `Slides.pdf` and publishes both to GitHub Pages. If you add slide assets outside `slides/img/` and `slides/themes/`, add them to the workflow's copy step.

## Shared skills

Use the shared skills from [codebytes/skills](https://github.com/codebytes/skills) (for Copilot CLI: `copilot plugin install https://github.com/codebytes/skills`). Don't copy their instructions or scripts into this repository.

| Task                                                                          | Skill               |
| ----------------------------------------------------------------------------- | ------------------- |
| Write or revise slides, notes, order, or layouts                              | `marp-authoring`    |
| Charts, Mermaid diagrams, or other visuals                                    | `marp-visuals`      |
| Editable `.drawio.svg` diagrams (stored in `slides/img/`)                     | `drawio-diagrams`   |
| Check overflow, clipping, and HTML/PDF parity before presenting or publishing | `marp-slide-review` |

## Build and run

```bash
dotnet build BuildWithAspire.slnx
dotnet test BuildWithAspire.slnx
dotnet run --project src/BuildWithAspire.AppHost   # starts the whole app and the Aspire dashboard
```

## Talk-specific rules

- Keep model and endpoint settings in configuration or user secrets; never commit Azure OpenAI keys.
- Update `docs/demo-guide.md` when a change alters the live demo steps.

## Readme

- Keep the readme sections in this order: Slides, Recordings, Resources, Contact.
- When a new recording of the talk is published, add it at the top of `## Recordings` as `- [Event Year](https://www.youtube.com/watch?v=VIDEO_ID)`. When the video's title differs from the talk's, add an em dash and the recorded title after the event. Keep the list newest first.
- The talk also appears on <https://chris-ayers.com/speaking/> (source: `content/speaking.md` in [codebytes/codebytes.github.io](https://github.com/codebytes/codebytes.github.io)); new events and recordings belong there too.

## General

- Keep changes scoped to the request; don't reformat unrelated slides or code.
- Never commit secrets, keys, or connection strings. Demo placeholders must be obviously fake.
