document.addEventListener("DOMContentLoaded", () => {

  // 1️⃣ YAZI DEĞİŞTİR (SADECE TEXT)
  const changeTextBtn = document.getElementById("changeTextBtn");
  const text = document.querySelector(".card__text");

  let changed = false;

  changeTextBtn.addEventListener("click", () => {
    if (!changed) {
      text.innerHTML =
        "Bu içerik JavaScript ile değiştirildi. Web sayfaları dinamik hale getirilebilir ve kullanıcı etkileşimlerine göre güncellenebilir.";
    } else {
      text.innerHTML =
        'Bu örnekte <strong>üst menü</strong>, <strong>yan menü</strong> ve ortada <strong>3x5 tablo</strong> bulunuyor.';
    }

    changed = !changed;
  });


  // 2️⃣ TABLO NOTA GÖRE SIRALAMA (FİNAL NOTUNA GÖRE)
  const sortBtn = document.getElementById("sortTableBtn");
  const tbody = document.querySelector(".dataTable tbody");

  let ascending = true;

  sortBtn.addEventListener("click", () => {
    const rows = Array.from(tbody.querySelectorAll("tr"));

    rows.sort((a, b) => {
      const finalA = parseInt(a.children[3].textContent);
      const finalB = parseInt(b.children[3].textContent);

      return ascending ? finalA - finalB : finalB - finalA;
    });

    tbody.innerHTML = "";

    rows.forEach((row, index) => {
      row.children[0].textContent = index + 1;
      tbody.appendChild(row);
    });

    ascending = !ascending;
  });


  // 3️⃣ KÜÇÜK RESİM DEĞİŞTİRME
  const changeImageBtn = document.getElementById("changeImageBtn");
  const miniImage = document.getElementById("miniImage");

  const images = [
    "images/img1.jpg",
    "images/img2.jpg",
    "images/img3.jpg"
  ];

  let currentIndex = 0;

  changeImageBtn.addEventListener("click", () => {
    currentIndex = (currentIndex + 1) % images.length;

    miniImage.style.opacity = 0;

    setTimeout(() => {
      miniImage.src = images[currentIndex];
      miniImage.style.opacity = 1;
    }, 200);
  });

});
document.addEventListener("DOMContentLoaded", () => {
  const box = document.querySelector(".imageEffectBox");
  const glow = document.querySelector(".imageGlow");

  if (box && glow) {
    box.addEventListener("mousemove", (e) => {
      const rect = box.getBoundingClientRect();
      const x = e.clientX - rect.left;
      const y = e.clientY - rect.top;

      glow.style.left = `${x}px`;
      glow.style.top = `${y}px`;
      glow.style.opacity = "1";
    });

    box.addEventListener("mouseleave", () => {
      glow.style.opacity = "0";
    });
  }
});