type PreviewTab = 'image' | 'model'

type CenterPanelProps = {
  activeTab: PreviewTab
  onTabChange: (tab: PreviewTab) => void
}

const tabBase =
  'flex items-center gap-2 border-b-2 px-1 pb-3 text-sm font-medium transition'

const toolButtons = [
  { icon: '⟳', label: '回転' },
  { icon: '✥', label: 'パン' },
  { icon: '＋', label: 'ズーム' },
  { icon: '⌂', label: 'フィット' },
]

function RobotShowcase() {
  return (
    <div className="relative flex h-full items-end justify-center overflow-hidden rounded-[28px] border border-white/10 bg-gradient-to-br from-[#18212e] via-[#11161e] to-[#0f1724] px-6 pb-8 pt-10">
      <div className="absolute inset-x-0 top-0 h-36 bg-[radial-gradient(circle_at_top,rgba(6,182,212,0.20),transparent_58%)]" />
      <div className="absolute inset-x-10 bottom-4 h-16 rounded-full bg-cyan-500/10 blur-3xl" />
      <div className="absolute left-6 top-6 rounded-full border border-white/10 bg-white/[0.05] px-3 py-1 text-xs uppercase tracking-[0.24em] text-[#8b949e]">
        Studio Render
      </div>
      <div className="relative flex h-[88%] w-full max-w-[340px] flex-col items-center justify-end">
        <div className="relative mb-3 h-28 w-28 rounded-full border border-slate-200/80 bg-white shadow-[0_18px_60px_rgba(255,255,255,0.08)]">
          <div className="absolute left-5 top-11 h-3 w-3 rounded-full bg-slate-950" />
          <div className="absolute right-5 top-11 h-3 w-3 rounded-full bg-slate-950" />
          <div className="absolute left-1/2 top-[52px] h-4 w-4 -translate-x-1/2 rounded-full bg-cyan-400 shadow-[0_0_18px_rgba(34,211,238,0.9)]" />
          <div className="absolute left-1/2 top-4 h-2 w-12 -translate-x-1/2 rounded-full bg-cyan-300/60" />
        </div>
        <div className="relative h-56 w-52 rounded-[2.5rem] border border-slate-300/80 bg-gradient-to-b from-white to-slate-200 shadow-[0_26px_80px_rgba(0,0,0,0.35)]">
          <div className="absolute inset-x-6 top-6 h-2 rounded-full bg-slate-900/80" />
          <div className="absolute left-1/2 top-12 h-16 w-16 -translate-x-1/2 rounded-[1.25rem] border border-slate-300 bg-gradient-to-b from-slate-50 to-slate-200" />
          <div className="absolute left-5 top-10 h-24 w-5 rounded-full bg-slate-950" />
          <div className="absolute right-5 top-10 h-24 w-5 rounded-full bg-slate-950" />
          <div className="absolute left-1/2 top-20 h-5 w-5 -translate-x-1/2 rounded-full bg-cyan-400 shadow-[0_0_25px_rgba(34,211,238,0.95)]" />
          <div className="absolute bottom-5 left-9 h-20 w-5 rounded-full bg-slate-900" />
          <div className="absolute bottom-5 right-9 h-20 w-5 rounded-full bg-slate-900" />
          <div className="absolute bottom-4 left-6 h-3 w-9 rounded-full bg-cyan-200/50 blur-[1px]" />
          <div className="absolute bottom-4 right-6 h-3 w-9 rounded-full bg-cyan-200/50 blur-[1px]" />
        </div>
      </div>
    </div>
  )
}

function ModelViewport() {
  return (
    <div
      className="relative flex h-full min-h-[320px] overflow-hidden rounded-[28px] border border-white/10 bg-[#0b1018]"
      style={{
        backgroundImage:
          'linear-gradient(rgba(255,255,255,0.04) 1px, transparent 1px), linear-gradient(90deg, rgba(255,255,255,0.04) 1px, transparent 1px), radial-gradient(circle at top, rgba(168,85,247,0.18), transparent 30%)',
        backgroundSize: '36px 36px, 36px 36px, auto',
        backgroundPosition: 'center center, center center, center top',
      }}
    >
      <div className="absolute inset-x-0 top-0 h-24 bg-gradient-to-b from-white/[0.06] to-transparent" />
      <div className="absolute left-5 top-1/2 z-10 flex -translate-y-1/2 flex-col gap-2">
        {toolButtons.map((tool) => (
          <button
            key={tool.label}
            type="button"
            className="flex min-w-20 items-center gap-2 rounded-xl border border-white/10 bg-[#161b22]/90 px-3 py-2 text-xs text-[#c9d1d9] shadow-lg transition hover:border-cyan-400/40 hover:text-white"
          >
            <span>{tool.icon}</span>
            <span>{tool.label}</span>
          </button>
        ))}
      </div>
      <div className="absolute right-5 top-5 z-10 flex gap-2">
        {['FRONT', 'RIGHT'].map((label) => (
          <button
            key={label}
            type="button"
            className="rounded-full border border-white/10 bg-[#161b22]/80 px-3 py-1.5 text-[11px] font-semibold tracking-[0.24em] text-[#c9d1d9]"
          >
            {label}
          </button>
        ))}
      </div>
      <div className="absolute left-1/2 top-[55%] h-52 w-52 -translate-x-1/2 -translate-y-1/2 rounded-full border border-cyan-400/10 bg-[radial-gradient(circle_at_center,rgba(34,211,238,0.18),rgba(11,16,24,0.15)_55%,transparent_70%)] blur-sm" />
      <div className="absolute bottom-10 left-1/2 h-28 w-[72%] -translate-x-1/2 rounded-[100%] border border-white/10 bg-gradient-to-b from-white/10 to-white/[0.02] shadow-[0_20px_80px_rgba(0,0,0,0.45)]" />
      <div className="absolute bottom-20 left-1/2 h-56 w-56 -translate-x-1/2 [perspective:1200px]">
        <div className="relative mx-auto h-full w-full animate-[spin_14s_linear_infinite] [transform-style:preserve-3d]">
          <div className="absolute left-1/2 top-3 h-20 w-20 -translate-x-1/2 rounded-full border border-slate-300/80 bg-white [transform:translateZ(44px)]" />
          <div className="absolute left-1/2 top-16 h-28 w-24 -translate-x-1/2 rounded-[2rem] border border-slate-300/80 bg-gradient-to-b from-slate-50 to-slate-300 [transform:translateZ(38px)]" />
          <div className="absolute left-10 top-20 h-20 w-4 rounded-full bg-slate-950 [transform:translateZ(28px)]" />
          <div className="absolute right-10 top-20 h-20 w-4 rounded-full bg-slate-950 [transform:translateZ(28px)]" />
          <div className="absolute bottom-10 left-[82px] h-20 w-4 rounded-full bg-slate-900 [transform:translateZ(20px)]" />
          <div className="absolute bottom-10 right-[82px] h-20 w-4 rounded-full bg-slate-900 [transform:translateZ(20px)]" />
          <div className="absolute left-1/2 top-[92px] h-4 w-4 -translate-x-1/2 rounded-full bg-cyan-400 shadow-[0_0_16px_rgba(34,211,238,0.95)] [transform:translateZ(52px)]" />
        </div>
      </div>
    </div>
  )
}

export function CenterPanel({ activeTab, onTabChange }: CenterPanelProps) {
  return (
    <section className="flex h-full min-w-0 flex-col rounded-[28px] border border-white/10 bg-[#161b22] p-5 shadow-[0_24px_80px_rgba(0,0,0,0.35)]">
      <div className="mb-5 flex items-center justify-between border-b border-white/10">
        <div className="flex items-center gap-6">
          <button
            type="button"
            onClick={() => onTabChange('image')}
            className={`${tabBase} ${
              activeTab === 'image'
                ? 'border-cyan-400 text-cyan-300'
                : 'border-transparent text-[#8b949e] hover:text-white'
            }`}
          >
            <span>🖼</span>
            <span>画像プレビュー</span>
          </button>
          <button
            type="button"
            onClick={() => onTabChange('model')}
            className={`${tabBase} ${
              activeTab === 'model'
                ? 'border-cyan-400 text-cyan-300'
                : 'border-transparent text-[#8b949e] hover:text-white'
            }`}
          >
            <span>⬡</span>
            <span>3Dプレビュー</span>
          </button>
        </div>
        <button type="button" className="mb-3 rounded-full border border-white/10 bg-white/[0.04] px-3 py-1.5 text-sm text-[#8b949e]">
          ⤢
        </button>
      </div>

      <div className="grid min-h-0 flex-1 gap-4 xl:grid-rows-[1.1fr_1fr]">
        <div className={`relative min-h-[320px] ${activeTab === 'image' ? 'ring-1 ring-cyan-400/40' : 'opacity-85'} rounded-[30px]`}>
          <RobotShowcase />
          <div className="absolute inset-x-4 bottom-4 flex gap-3">
            <button
              type="button"
              className="rounded-full border border-white/10 bg-[#161b22]/85 px-4 py-2 text-sm text-[#c9d1d9] backdrop-blur transition hover:border-cyan-400/40 hover:text-white"
            >
              ↻ リセット
            </button>
            <button
              type="button"
              className="rounded-full border border-cyan-400/20 bg-cyan-500/10 px-4 py-2 text-sm text-cyan-300 backdrop-blur transition hover:border-cyan-300/40 hover:text-cyan-200"
            >
              ⚡ バリエーション
            </button>
          </div>
        </div>

        <div className={`relative min-h-[320px] ${activeTab === 'model' ? 'ring-1 ring-purple-400/45' : 'opacity-95'} rounded-[30px]`}>
          <ModelViewport />
        </div>
      </div>

      <button
        type="button"
        className="mt-5 w-full rounded-2xl bg-gradient-to-r from-cyan-500 to-purple-500 px-5 py-3.5 text-sm font-semibold text-white shadow-[0_20px_50px_rgba(99,102,241,0.28)] transition hover:brightness-110"
      >
        ↓ GLBをダウンロード ▾
      </button>
    </section>
  )
}
