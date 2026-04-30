/* ══════════════════════════════════════════════════════════════
   Swipe Animations — Paw Trails, Emojis, Sound Texts
   Called from Blazor via JS interop
   ══════════════════════════════════════════════════════════════ */

window.swipeAnimations = {

  /* ── SVG Paw Print Factories ── */

  _createDogPaw(color) {
    const el = document.createElement('div');
    el.className = 'swipe-print';
    el.innerHTML = `<svg width="36" height="36" viewBox="0 0 40 40">
      <ellipse cx="20" cy="26" rx="10" ry="12" fill="${color}" opacity="0.8"/>
      <ellipse cx="10" cy="12" rx="5.5" ry="6.5" fill="${color}" opacity="0.8"/>
      <ellipse cx="20" cy="8"  rx="5"   ry="6"   fill="${color}" opacity="0.8"/>
      <ellipse cx="30" cy="12" rx="5.5" ry="6.5" fill="${color}" opacity="0.8"/>
    </svg>`;
    return el;
  },

  _createCatPaw(color) {
    const el = document.createElement('div');
    el.className = 'swipe-print';
    el.innerHTML = `<svg width="26" height="26" viewBox="0 0 32 32">
      <ellipse cx="16" cy="20" rx="7"   ry="9"   fill="${color}" opacity="0.7"/>
      <ellipse cx="8"  cy="10" rx="4"   ry="5"   fill="${color}" opacity="0.7"/>
      <ellipse cx="16" cy="7"  rx="3.5" ry="4.5" fill="${color}" opacity="0.7"/>
      <ellipse cx="24" cy="10" rx="4"   ry="5"   fill="${color}" opacity="0.7"/>
    </svg>`;
    return el;
  },

  /* ── Emoji Pools ── */

  _adoptEmojis: ['\u2764\uFE0F','\uD83D\uDC96','\uD83D\uDC95','\uD83C\uDF89','\u2728','\uD83D\uDC97','\uD83E\uDE77','\uD83C\uDF8A','\uD83D\uDC9D','\uD83E\uDD73'],
  _rejectEmojis: ['\uD83D\uDE22','\uD83D\uDE3F','\uD83E\uDD7A','\uD83D\uDE1E','\uD83D\uDC94'],

  /* ── Trail Config per animal type ── */

  _trails: {
    dog: {
      adopt: {
        color: '#d4a574', factory: '_createDogPaw',
        count: 7, spacing: 36, sway: 18,
        rotBase: -15, rotRange: 8, peakOpacity: 0.75,
        anim: 'swipe-print-stamp', stagger: 0.11,
        sounds: [
          { text: 'Woof!', offset: 0.28, color: '#fbbf24', yOff: -55 },
          { text: '*tap tap tap*', offset: 0.65, color: '#d4a574', yOff: 40 },
        ],
      },
      reject: {
        color: '#7a6050', factory: '_createDogPaw',
        count: 5, spacing: 32, sway: 14,
        rotBase: 10, rotRange: 10, peakOpacity: 0.45,
        anim: 'swipe-print-reject', stagger: 0.09,
        sounds: [
          { text: '*whimper*', offset: 0.3, color: '#9ca3af', yOff: -45 },
        ],
      },
    },
    cat: {
      adopt: {
        color: '#c0a0d0', factory: '_createCatPaw',
        count: 9, spacing: 26, sway: 5,
        rotBase: -8, rotRange: 6, peakOpacity: 0.65,
        anim: 'swipe-print-stamp', stagger: 0.08,
        sounds: [
          { text: 'Prr~', offset: 0.25, color: '#c4b5fd', yOff: -45 },
          { text: '*tip tip tip*', offset: 0.6, color: '#a78bfa', yOff: 35 },
        ],
      },
      reject: {
        color: '#6a5a70', factory: '_createCatPaw',
        count: 6, spacing: 22, sway: 4,
        rotBase: 5, rotRange: 8, peakOpacity: 0.4,
        anim: 'swipe-print-reject', stagger: 0.07,
        sounds: [
          { text: 'Hisss~', offset: 0.25, color: '#9ca3af', yOff: -40 },
        ],
      },
    },
  },

  /* ── Get trail config, fallback to dog for unknown types ── */

  _getTrail(animalType, action) {
    const key = (animalType || 'dog').toLowerCase();
    const animal = this._trails[key] || this._trails.dog;
    return animal[action];
  },

  /* ── Spawn emoji particles at random positions ── */

  _spawnEmojis(area, action) {
    const isAdopt = action === 'adopt';
    const pool = isAdopt ? this._adoptEmojis : this._rejectEmojis;
    const anim = isAdopt ? 'swipe-emoji-pop-up' : 'swipe-emoji-sink';
    const count = isAdopt ? 14 : 10;
    const w = area.clientWidth;
    const h = area.clientHeight;

    for (let i = 0; i < count; i++) {
      const el = document.createElement('div');
      el.className = 'swipe-emoji-particle';
      el.textContent = pool[Math.floor(Math.random() * pool.length)];

      const x = 10 + Math.random() * (w - 40);
      const y = 20 + Math.random() * (h - 60);
      const size = 18 + Math.random() * 18;
      const rot = -25 + Math.random() * 50;
      const delay = Math.random() * 0.6;
      const dur = 0.9 + Math.random() * 0.5;

      el.style.cssText = `left:${x}px;top:${y}px;font-size:${size}px;--r:${rot}deg;animation:${anim} ${dur}s ease-out ${delay}s forwards;`;
      area.appendChild(el);
    }
  },

  /* ── Create sound text element ── */

  _createSound(text, x, y, color, delay) {
    const el = document.createElement('div');
    el.className = 'swipe-sound';
    el.textContent = text;
    el.style.cssText = `left:${x}px;top:${y}px;color:${color};animation:swipe-sound-float 0.9s ease-out ${delay}s forwards;`;
    return el;
  },

  /* ══════════════════════════════════════════════════════════════
     Main entry point — called from Blazor JS interop
     Returns a promise that resolves when the animation is done
     ══════════════════════════════════════════════════════════════ */

  playSwipeAnimation(containerSelector, action, animalType, dragOffsetX) {
    return new Promise((resolve) => {
      const container = document.querySelector(containerSelector);
      if (!container) { resolve(); return; }

      const area = container.querySelector('.swipe-card-area');
      const card = container.querySelector('.swipe-top-card');
      if (!area || !card) { resolve(); return; }

      const cfg = this._getTrail(animalType, action);
      const isAdopt = action === 'adopt';
      const dir = isAdopt ? 1 : -1;

      // Clean any previous animation elements
      area.querySelectorAll('.swipe-print, .swipe-sound, .swipe-emoji-particle').forEach(el => el.remove());

      const areaW = area.clientWidth;
      const centerY = area.clientHeight / 2;
      const startX = isAdopt
        ? areaW * 0.12
        : areaW * 0.88 - cfg.count * cfg.spacing;

      // 1. Emojis
      this._spawnEmojis(area, action);

      // 2. Card exit from current drag position
      const fromX = dragOffsetX || 0;
      const exitX = isAdopt ? 350 : -350;
      const exitRot = isAdopt ? 15 : -15;

      card.style.transition = 'none';
      card.style.transform = `translateX(${fromX}px) rotate(${(fromX / 300) * 12}deg)`;
      card.style.opacity = '1';
      card.offsetHeight; // force reflow
      card.classList.add('swipe-card-exiting');
      card.style.pointerEvents = 'none';
      card.style.transform = `translateX(${exitX}px) rotate(${exitRot}deg)`;
      card.style.opacity = '0.15';

      // 3. Paw prints
      for (let i = 0; i < cfg.count; i++) {
        const print = this[cfg.factory](cfg.color);
        const x = startX + i * cfg.spacing * dir;
        const yOff = (i % 2 === 0 ? -1 : 1) * cfg.sway;
        const rot = cfg.rotBase * dir + (Math.random() * cfg.rotRange - cfg.rotRange / 2);

        print.style.left = x + 'px';
        print.style.top = (centerY + yOff - 18) + 'px';
        print.style.setProperty('--rot', rot + 'deg');
        print.style.setProperty('--peak', String(cfg.peakOpacity));
        print.style.animation = `${cfg.anim} 1s ease-out ${i * cfg.stagger}s forwards`;
        area.appendChild(print);
      }

      // 4. Sounds
      for (const s of cfg.sounds) {
        const sx = startX + cfg.count * cfg.spacing * dir * s.offset;
        area.appendChild(this._createSound(s.text, sx, centerY + s.yOff, s.color, s.offset));
      }

      // 5. Resolve after animation completes
      const totalDuration = cfg.count * cfg.stagger * 1000 + 1200;
      setTimeout(() => {
        area.querySelectorAll('.swipe-print, .swipe-sound, .swipe-emoji-particle').forEach(el => el.remove());
        if (card) {
          card.classList.remove('swipe-card-exiting');
          card.style.transition = 'none';
          card.style.transform = '';
          card.style.opacity = '';
          card.style.pointerEvents = '';
        }
        resolve();
      }, totalDuration);
    });
  }
};
