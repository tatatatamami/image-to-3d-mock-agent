import type { ReactNode } from 'react'

type StepState = 'done' | 'active' | 'pending'

type Step = {
  title: string
  subtitle: string
  state: StepState
  number: string
}

const steps: Step[] = [
  { title: '指示', subtitle: 'プロンプトを入力', state: 'done', number: '1' },
  { title: '画像生成', subtitle: 'AIが画像を生成', state: 'done', number: '2' },
  { title: '画像確認', subtitle: '画像を確認・調整', state: 'done', number: '3' },
  { title: '3D生成', subtitle: '画像から3Dモデルを生成', state: 'active', number: '4' },
  { title: '出力', subtitle: 'ダウンロード・共有', state: 'pending', number: '5' },
]

const styles = [
  { emoji: '📷', label: 'Realistic', active: false },
  { emoji: '🧸', label: 'Toy', active: true },
  { emoji: '🎭', label: 'Anime', active: false },
  { emoji: '🤖', label: 'Mecha', active: false },
  { emoji: '🔥', label: 'Fantasy', active: false },
]

const historyDates = ['2024/05/24 14:32', '2024/05/24 13:08', '2024/05/24 11:52']

function Card({ children }: { children: ReactNode }) {
  return <div className="rounded-[26px] border border-white/10 bg-[#161b22] p-5 shadow-[0_24px_80px_rgba(0,0,0,0.35)]">{children}</div>
}

function ToggleGroup({ options }: { options: { label: string; active?: boolean }[] }) {
  return (
    <div className="grid grid-cols-3 gap-2">
      {options.map((option) => (
        <button
          key={option.label}
          type="button"
          className={`rounded-xl border px-3 py-2 text-sm transition ${
            option.active
              ? 'border-cyan-400/40 bg-cyan-500/10 text-cyan-300'
              : 'border-white/10 bg-white/[0.03] text-[#c9d1d9] hover:border-white/20 hover:text-white'
          }`}
        >
          {option.label}
        </button>
      ))}
    </div>
  )
}

function StepIcon({ step }: { step: Step }) {
  if (step.state === 'done') {
    return (
      <div className="flex h-9 w-9 items-center justify-center rounded-full bg-cyan-500 text-sm font-semibold text-white shadow-[0_0_18px_rgba(6,182,212,0.45)]">
        ✓
      </div>
    )
  }

  if (step.state === 'active') {
    return (
      <div className="relative flex h-9 w-9 items-center justify-center rounded-full border border-purple-400/40 bg-purple-500/20 text-sm font-semibold text-purple-200">
        <span className="absolute inset-0 animate-spin rounded-full border-2 border-purple-300/10 border-t-purple-300" />
        <span className="relative">{step.number}</span>
      </div>
    )
  }

  return (
    <div className="flex h-9 w-9 items-center justify-center rounded-full border border-white/10 bg-white/[0.04] text-sm font-semibold text-[#8b949e]">
      {step.number}
    </div>
  )
}

function MiniThumb({ variant }: { variant: number }) {
  const backgrounds = [
    'from-cyan-500/15 to-slate-800',
    'from-purple-500/18 to-slate-800',
    'from-sky-500/16 to-slate-800',
  ]

  return (
    <div className={`relative h-24 overflow-hidden rounded-2xl border border-white/10 bg-gradient-to-br ${backgrounds[variant]}`}>
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_top,rgba(255,255,255,0.16),transparent_45%)]" />
      <div className="absolute left-1/2 top-6 h-10 w-10 -translate-x-1/2 rounded-full border border-slate-200/70 bg-white" />
      <div className="absolute left-1/2 top-14 h-8 w-12 -translate-x-1/2 rounded-[1rem] border border-slate-300/70 bg-gradient-to-b from-slate-50 to-slate-200" />
      <div className="absolute left-[28%] top-14 h-5 w-2 rounded-full bg-slate-900" />
      <div className="absolute right-[28%] top-14 h-5 w-2 rounded-full bg-slate-900" />
      <div className="absolute left-1/2 top-[54px] h-2.5 w-2.5 -translate-x-1/2 rounded-full bg-cyan-400 shadow-[0_0_10px_rgba(34,211,238,0.8)]" />
    </div>
  )
}

export function RightPanel() {
  return (
    <aside className="flex h-full min-w-0 flex-col gap-4">
      <Card>
        <div className="mb-5 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-[#f0f6fc]">ワークフロー</h2>
          <span className="text-xs uppercase tracking-[0.24em] text-purple-300">Live</span>
        </div>
        <div className="space-y-4">
          {steps.map((step, index) => (
            <div key={step.number} className="flex gap-3">
              <div className="flex flex-col items-center">
                <StepIcon step={step} />
                {index < steps.length - 1 ? <div className="mt-2 h-10 w-px bg-white/10" /> : null}
              </div>
              <div className="pt-1">
                <div className="text-sm font-medium text-[#f0f6fc]">{step.title}</div>
                <div className={`mt-1 text-sm ${step.state === 'active' ? 'text-purple-300' : 'text-[#8b949e]'}`}>
                  {step.subtitle}
                </div>
              </div>
            </div>
          ))}
        </div>
      </Card>

      <Card>
        <div className="mb-5 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-[#f0f6fc]">生成設定</h2>
          <button type="button" className="text-sm text-cyan-300 transition hover:text-cyan-200">
            詳細設定
          </button>
        </div>

        <div className="space-y-5">
          <div>
            <div className="mb-3 flex items-center gap-2 text-sm font-medium text-[#f0f6fc]">
              <span>品質</span>
              <span className="text-xs text-[#8b949e]">ℹ</span>
            </div>
            <ToggleGroup options={[{ label: 'Preview' }, { label: 'Standard', active: true }, { label: 'High' }]} />
          </div>

          <div>
            <div className="mb-3 flex items-center gap-2 text-sm font-medium text-[#f0f6fc]">
              <span>スタイル</span>
              <span className="text-xs text-[#8b949e]">ℹ</span>
            </div>
            <div className="grid grid-cols-3 gap-2">
              {styles.map((style) => (
                <button
                  key={style.label}
                  type="button"
                  className={`flex flex-col items-center gap-1 rounded-2xl border px-3 py-3 text-xs transition ${
                    style.active
                      ? 'border-purple-400/40 bg-purple-500/10 text-purple-200'
                      : 'border-white/10 bg-white/[0.03] text-[#c9d1d9] hover:border-white/20 hover:text-white'
                  }`}
                >
                  <span className="text-lg">{style.emoji}</span>
                  <span>{style.label}</span>
                </button>
              ))}
            </div>
          </div>

          <div>
            <div className="mb-3 flex items-center gap-2 text-sm font-medium text-[#f0f6fc]">
              <span>背景</span>
              <span className="text-xs text-[#8b949e]">ℹ</span>
            </div>
            <div className="grid grid-cols-3 gap-2">
              {['白', '透明', '自動'].map((label) => (
                <button
                  key={label}
                  type="button"
                  className={`rounded-xl border px-3 py-2 text-sm transition ${
                    label === '自動'
                      ? 'border-cyan-400/40 bg-cyan-500/10 text-cyan-300'
                      : 'border-white/10 bg-white/[0.03] text-[#c9d1d9] hover:border-white/20 hover:text-white'
                  }`}
                >
                  {label}
                </button>
              ))}
            </div>
          </div>
        </div>
      </Card>

      <Card>
        <div className="mb-5 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-[#f0f6fc]">最近の履歴</h2>
          <button type="button" className="text-sm text-cyan-300 transition hover:text-cyan-200">
            すべて見る
          </button>
        </div>
        <div className="grid grid-cols-3 gap-3">
          {historyDates.map((date, index) => (
            <div key={date} className="space-y-2">
              <MiniThumb variant={index} />
              <p className="text-[11px] leading-5 text-[#8b949e]">{date}</p>
            </div>
          ))}
        </div>
      </Card>
    </aside>
  )
}
