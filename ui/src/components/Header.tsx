const iconButton =
  'flex h-9 w-9 items-center justify-center rounded-full border border-white/10 bg-white/5 text-sm text-[#8b949e] transition hover:border-cyan-400/40 hover:text-[#f0f6fc]'

function CubeLogo() {
  return (
    <div className="relative h-10 w-10 rounded-2xl border border-cyan-400/30 bg-cyan-500/10 shadow-[0_0_30px_rgba(6,182,212,0.18)]">
      <div className="absolute inset-[9px] rotate-45 rounded-[6px] border border-cyan-300/80 bg-gradient-to-br from-cyan-300 to-blue-500" />
      <div className="absolute left-1/2 top-[6px] h-3.5 w-3.5 -translate-x-1/2 rotate-45 rounded-[4px] border border-white/40 bg-cyan-200/20" />
    </div>
  )
}

export function Header() {
  return (
    <header className="fixed inset-x-0 top-0 z-50 border-b border-white/10 bg-[#0d1117]/90 backdrop-blur-xl">
      <div className="mx-auto flex h-20 max-w-[1720px] items-center justify-between px-4">
        <div className="flex items-center gap-4">
          <CubeLogo />
          <div>
            <div className="flex items-center gap-3">
              <h1 className="text-lg font-semibold tracking-wide text-[#f0f6fc]">3D Mock Studio</h1>
              <span className="rounded-full border border-cyan-400/20 bg-cyan-500/10 px-2.5 py-1 text-[11px] font-medium uppercase tracking-[0.28em] text-cyan-300">
                AI Creative Studio
              </span>
            </div>
            <p className="mt-1 text-sm text-[#8b949e]">Concept to 3D &gt;</p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <div className="hidden items-center gap-2 rounded-full border border-green-500/25 bg-green-500/10 px-4 py-2 text-sm font-medium text-green-400 md:flex">
            <span className="text-xs">●</span>
            <span>生成完了</span>
          </div>
          <button type="button" className={iconButton} aria-label="Help">
            ?
          </button>
          <button type="button" className={iconButton} aria-label="Notifications">
            🔔
          </button>
          <div className="flex items-center gap-2 rounded-full border border-white/10 bg-white/5 py-1 pl-1 pr-3">
            <div className="flex h-9 w-9 items-center justify-center rounded-full bg-gradient-to-br from-cyan-400 to-purple-500 text-sm font-semibold text-white">
              Y
            </div>
            <span className="text-sm text-[#8b949e]">▾</span>
          </div>
        </div>
      </div>
    </header>
  )
}
