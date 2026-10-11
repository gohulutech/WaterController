# water-controller-web

React 19 + Vite frontend for the Water Controller project.

## Deployment

The frontend is deployed automatically by GitHub Actions:
[`.github/workflows/water-controller-web-deploy.yml`](../.github/workflows/water-controller-web-deploy.yml).

- **Trigger:** push to `main` that changes anything under `water-controller-web/**`.
- **Runs on:** a self-hosted runner with labels `self-hosted, linux, ARM64`.
- **Pipeline:** Node 24 → `corepack enable` → copy `/opt/water-controller-web/.env.production` → `pnpm install --frozen-lockfile` → `pnpm build` → publish `dist/` to `/var/www/water-controller-web`.
- **Serving:** static SPA served from `/var/www/water-controller-web` (no service restart needed after a deploy).

### Self-hosted runner

The Actions runner is installed at `/opt/actions-runner` on the deploy host.
It must be running for a deploy to start — otherwise the job stays queued ("Waiting for a runner to pick up this job").

Start (or restart) it manually:

```bash
cd /opt/actions-runner/
./run.sh
```

Confirm it shows as **Idle** in GitHub → repo → **Settings → Actions → Runners**.

### Environment variables

`VITE_API_URL` (used in `src/api/consumption.ts`) is a **build-time** variable — Vite inlines it into the bundle, so it is not read at runtime.

- The production value comes from `/opt/water-controller-web/.env.production` **on the runner/server**, not from the repo.
- Editing that file has **no effect until the build runs again**. To apply a change, re-run the **Deploy Frontend** workflow (Actions tab → *Re-run all jobs*) or push a change under `water-controller-web/**`.

---

## React + Vite template notes

This template provides a minimal setup to get React working in Vite with HMR and some ESLint rules.

Currently, two official plugins are available:

- [@vitejs/plugin-react](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react) uses [Oxc](https://oxc.rs)
- [@vitejs/plugin-react-swc](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react-swc) uses [SWC](https://swc.rs/)

## React Compiler

The React Compiler is not enabled on this template because of its impact on dev & build performances. To add it, see [this documentation](https://react.dev/learn/react-compiler/installation).

## Expanding the ESLint configuration

If you are developing a production application, we recommend using TypeScript with type-aware lint rules enabled. Check out the [TS template](https://github.com/vitejs/vite/tree/main/packages/create-vite/template-react-ts) for information on how to integrate TypeScript and [`typescript-eslint`](https://typescript-eslint.io) in your project.
