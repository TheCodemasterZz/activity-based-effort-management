export function Footer() {
  return (
    <footer className="flex items-center justify-between border-t border-slate-200 bg-white px-6 py-3 text-xs text-slate-400">
      <span>Copyright © {new Date().getFullYear()} Barış Kalaycıoğlu. All rights reserved.</span>
      <span>
        Made with <span className="text-red-500">❤</span> in Türkiye for Türk Kızılay.
      </span>
    </footer>
  );
}
