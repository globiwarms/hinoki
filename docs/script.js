/* Hinoki — living details */
document.documentElement.classList.add("js");

// ---- scroll reveal ----
const observer = new IntersectionObserver((entries) => {
  entries.forEach((e) => {
    if (e.isIntersecting) {
      e.target.classList.add("shown");
      observer.unobserve(e.target);
    }
  });
}, { threshold: 0.15, rootMargin: "0px 0px -40px 0px" });

document.querySelectorAll(".card, .step, .shot, .section-head, .cta-card")
  .forEach((el, i) => {
    el.style.transitionDelay = (i % 3) * 0.07 + "s";
    observer.observe(el);
  });

// ---- flower reacts to the cursor ----
const flower = document.getElementById("flower");
if (flower && !matchMedia("(prefers-reduced-motion: reduce)").matches) {
  const petals = flower.querySelectorAll(".petal");
  const state = new Map([...petals].map((p) => [p, { r: 0, lift: 0 }]));
  let hovered = null;

  petals.forEach((p) => {
    p.addEventListener("mouseenter", () => { hovered = p; });
    p.addEventListener("mouseleave", () => { if (hovered === p) hovered = null; });
  });

  // gentle breathing on hover of the whole flower
  flower.addEventListener("pointermove", (e) => {
    const rect = flower.getBoundingClientRect();
    const dx = (e.clientX - rect.left) / rect.width - 0.5;
    const dy = (e.clientY - rect.top) / rect.height - 0.5;
    flower.style.setProperty("--tx", dx * 14 + "px");
    flower.style.setProperty("--ty", dy * 14 + "px");
  });
  flower.addEventListener("pointerleave", () => {
    flower.style.setProperty("--tx", "0px");
    flower.style.setProperty("--ty", "0px");
  });

  // petals click → pulse like in the app
  petals.forEach((p) => {
    p.style.cursor = "pointer";
    p.addEventListener("click", () => {
      p.animate(
        [{ transform: p.style.transform }, { filter: "brightness(1.5)" }],
        { duration: 300, easing: "ease-out" }
      );
    });
  });

  // parallax loop (transform on wrapper to keep petal rotation intact)
  let tx = 0, ty = 0;
  (function drift() {
    const cur = getComputedStyle(flower).getPropertyValue("--tx") || "0px";
    const cury = getComputedStyle(flower).getPropertyValue("--ty") || "0px";
    tx += (parseFloat(cur) - tx) * 0.06;
    ty += (parseFloat(cury) - ty) * 0.06;
    flower.style.translate = tx + "px " + ty + "px";
    requestAnimationFrame(drift);
  })();
}

// ---- nav shrinks after scroll ----
const nav = document.querySelector(".nav");
addEventListener("scroll", () => {
  if (scrollY > 40) nav.style.background = "rgba(14,16,18,0.78)";
  else nav.style.background = "rgba(14,16,18,0.55)";
}, { passive: true });
