import * as THREE from "three";
import * as OBC from "@thatopen/components";
import * as FRAGS from "@thatopen/fragments";

// ── 1. Setup: container + Components ────────────────────────────────────────
const container = document.getElementById("app") as HTMLElement;
const statusEl  = document.getElementById("status") as HTMLElement;

function setStatus(msg: string, type: "info" | "error" | "ok" = "info") {
  statusEl.textContent = msg;
  statusEl.className  = "show " + (type === "info" ? "" : type);
}

// ── 2. Create OBC World (Three.js scene + camera + renderer) ─────────────────
const components = new OBC.Components();
const worlds     = components.get(OBC.Worlds);

const world = worlds.create<
  OBC.SimpleScene,
  OBC.SimpleCamera,
  OBC.SimpleRenderer
>();

world.scene    = new OBC.SimpleScene(components);
world.renderer = new OBC.SimpleRenderer(components, container);
world.camera   = new OBC.SimpleCamera(components);

components.init();

// ── 3. Scene defaults ────────────────────────────────────────────────────────
world.scene.setup();
world.scene.three.background = new THREE.Color(0x1a1a2e);

const ambientLight = new THREE.AmbientLight(0xffffff, 0.7);
const dirLight     = new THREE.DirectionalLight(0xffffff, 1.5);
dirLight.position.set(10, 20, 10);
world.scene.three.add(ambientLight, dirLight);

// Add a simple grid for orientation reference
const grid = components.get(OBC.Grids);
grid.create(world);

// ── 4. Initial camera position ────────────────────────────────────────────────
world.camera.controls.setLookAt(20, 10, 20, 0, 0, 0);

// ── 5. Fragments Manager  ─────────────────────────────────────────────────────
// FragmentsManager (OBC v3) manages loading / unloading of .frag models
const fragmentsManager = components.get(OBC.FragmentsManager);

// ── 6. File input handler ─────────────────────────────────────────────────────
const fileInput = document.getElementById("file-input") as HTMLInputElement;

fileInput.addEventListener("change", async (event) => {
  const input = event.target as HTMLInputElement;
  if (!input.files || input.files.length === 0) return;

  const file = input.files[0];
  setStatus(`Loading ${file.name}…`);

  try {
    // Read the file as an ArrayBuffer → Uint8Array
    const arrayBuffer = await file.arrayBuffer();
    const bytes       = new Uint8Array(arrayBuffer);

    // Load into the FragmentsManager
    // This returns a FragmentsGroup which is a THREE.Group already in the scene
    const model = fragmentsManager.load(bytes);

    // Add to the world scene so it renders
    world.scene.three.add(model);

    // Fit camera to bounding box of loaded model
    const boundingBox = new THREE.Box3().setFromObject(model);
    const center      = new THREE.Vector3();
    const size        = new THREE.Vector3();
    boundingBox.getCenter(center);
    boundingBox.getSize(size);

    const maxDim  = Math.max(size.x, size.y, size.z);
    const camDist = maxDim * 1.8;

    await world.camera.controls.setLookAt(
      center.x + camDist,
      center.y + camDist * 0.5,
      center.z + camDist,
      center.x, center.y, center.z,
      true // animate
    );

    setStatus(`✔ ${file.name} loaded (${model.items.length} fragments)`, "ok");
  } catch (err: any) {
    console.error(err);
    setStatus(`Error: ${err.message}`, "error");
  }
});
