import type { FC } from "react";
import {
  AbsoluteFill,
  Easing,
  interpolate,
  useCurrentFrame,
} from "remotion";

type ModuleFlow = {
  title: string;
  accent: string;
  glow: string;
  side: "left" | "right";
  steps: string[];
};

const modules: ModuleFlow[] = [
  {
    title: "PROCUREMENT",
    accent: "#00e5ff",
    glow: "rgba(0,229,255,0.35)",
    side: "left",
    steps: ["PR", "RFQ", "PO", "ASN", "GR", "AP"],
  },
  {
    title: "INVENTORY",
    accent: "#72f1b8",
    glow: "rgba(114,241,184,0.32)",
    side: "right",
    steps: ["SKU", "LOT", "ISSUE", "RETURN", "TRANSFER", "ALERT"],
  },
  {
    title: "MANUFACTURING",
    accent: "#ff8a00",
    glow: "rgba(255,138,0,0.35)",
    side: "left",
    steps: ["BOM", "ROUTING", "WO", "WIP", "REPORT", "DONE"],
  },
  {
    title: "QUALITY",
    accent: "#ff4d6d",
    glow: "rgba(255,77,109,0.35)",
    side: "right",
    steps: ["IQC", "IPQC", "TRACE", "OQC", "NCR", "CAPA"],
  },
  {
    title: "FINANCE",
    accent: "#ffd84d",
    glow: "rgba(255,216,77,0.34)",
    side: "left",
    steps: ["COST", "BUDGET", "INVOICE", "PAYABLE", "RECEIPT", "KPI"],
  },
  {
    title: "HR",
    accent: "#9b7bff",
    glow: "rgba(155,123,255,0.34)",
    side: "right",
    steps: ["STAFF", "SHIFT", "CLOCK", "LEAVE", "PAYROLL", "AI"],
  },
];

const clampText = (frame: number, start: number, end: number) => {
  return interpolate(frame, [start, end], [0, 1], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
    easing: Easing.bezier(0.2, 0.8, 0.2, 1),
  });
};

const gridLines = Array.from({ length: 18 });
const verticalLines = Array.from({ length: 32 });
const VIDEO_WIDTH = 1920;
const VIDEO_HEIGHT = 1080;

const BattlefieldBackground: FC = () => {
  const frame = useCurrentFrame();
  const pulse = 0.75 + Math.sin(frame / 9) * 0.08;

  return (
    <AbsoluteFill
      style={{
        background:
          "radial-gradient(circle at 50% 50%, rgba(22,42,86,0.9) 0%, rgba(6,8,18,1) 42%, rgba(0,0,0,1) 100%)",
        overflow: "hidden",
      }}
    >
      <div className="noise-layer" />
      <div
        style={{
          position: "absolute",
          inset: -200,
          background:
            "conic-gradient(from 180deg at 50% 50%, rgba(0,229,255,0.12), rgba(255,77,109,0.06), rgba(114,241,184,0.1), rgba(255,138,0,0.08), rgba(0,229,255,0.12))",
          filter: "blur(60px)",
          transform: `rotate(${frame * 0.35}deg) scale(${pulse})`,
        }}
      />
      {gridLines.map((_, index) => (
        <div
          key={`h-${index}`}
          style={{
            position: "absolute",
            left: 0,
            right: 0,
            top: `${(index + 1) * (VIDEO_HEIGHT / 19)}px`,
            height: 1,
            opacity: 0.15,
            background:
              "linear-gradient(90deg, transparent 0%, rgba(118,238,255,0.5) 40%, rgba(118,238,255,0.1) 100%)",
            transform: `translateY(${Math.sin(frame / 8 + index) * 6}px)`,
          }}
        />
      ))}
      {verticalLines.map((_, index) => (
        <div
          key={`v-${index}`}
          style={{
            position: "absolute",
            top: 0,
            bottom: 0,
            left: `${(index + 1) * (VIDEO_WIDTH / 33)}px`,
            width: 1,
            opacity: 0.08,
            background:
              "linear-gradient(180deg, transparent 0%, rgba(255,255,255,0.55) 45%, transparent 100%)",
          }}
        />
      ))}
      <div
        style={{
          position: "absolute",
          inset: 0,
          background:
            "linear-gradient(90deg, rgba(0,229,255,0.16) 0%, transparent 24%, transparent 76%, rgba(255,77,109,0.16) 100%)",
          mixBlendMode: "screen",
        }}
      />
    </AbsoluteFill>
  );
};

const HeaderBlast: FC = () => {
  const frame = useCurrentFrame();
  const titleIn = interpolate(frame, [0, 20], [0, 1], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
    easing: Easing.bezier(0.16, 0.84, 0.24, 1),
  });
  const subtitleIn = clampText(frame, 12, 40);
  const flash = interpolate(frame, [0, 6, 18], [1, 0.3, 0], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });

  return (
    <AbsoluteFill style={{ justifyContent: "center", alignItems: "center" }}>
      <div
        style={{
          position: "absolute",
          inset: 0,
          background: `radial-gradient(circle, rgba(255,255,255,${flash}) 0%, rgba(255,255,255,0) 55%)`,
        }}
      />
      <div
        style={{
          transform: `scale(${0.78 + titleIn * 0.22}) translateY(${(1 - titleIn) * 80}px)`,
          opacity: titleIn,
          textAlign: "center",
        }}
      >
        <div className="eyebrow">ERP FLOW</div>
        <div className="hero-title">AI MODULE BATTLE</div>
        <div
          style={{
            marginTop: 28,
            color: "rgba(218,240,255,0.92)",
            fontSize: 42,
            letterSpacing: 10,
            opacity: subtitleIn,
          }}
        >
          採購 × 庫存 × 生產 × 品質 × 財務 × HR
        </div>
      </div>
    </AbsoluteFill>
  );
};

const ModuleCard: FC<{ module: ModuleFlow; start: number; index: number }> = ({
  module,
  start,
  index,
}) => {
  const frame = useCurrentFrame();
  const local = frame - start;
  const enter = interpolate(local, [0, 14], [0, 1], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
    easing: Easing.bezier(0.16, 0.84, 0.24, 1),
  });
  const exit = interpolate(local, [34, 42], [1, 0], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });
  const opacity = enter * exit;
  const xDirection = module.side === "left" ? -1 : 1;
  const translateX = (1 - enter) * 380 * xDirection;
  const scan = ((local * 34) % 1100) - 120;
  const edgeStyle = module.side === "left" ? { left: 0 } : { right: 0 };

  return (
    <AbsoluteFill
      style={{
        justifyContent: "center",
        alignItems: module.side === "left" ? "flex-start" : "flex-end",
        padding: "0 110px",
        opacity,
        transform: `translateX(${translateX}px)`,
      }}
    >
      <div
        style={{
          width: 980,
          height: 310,
          position: "relative",
          borderRadius: 34,
          background:
            "linear-gradient(135deg, rgba(8,14,24,0.88) 0%, rgba(9,22,42,0.76) 48%, rgba(2,8,18,0.94) 100%)",
          border: `1px solid ${module.accent}88`,
          boxShadow: `0 0 60px ${module.glow}, inset 0 0 0 1px rgba(255,255,255,0.05)`,
          overflow: "hidden",
        }}
      >
        <div
          style={{
            position: "absolute",
            inset: 0,
            background: `linear-gradient(120deg, transparent 0%, ${module.accent}1c 35%, transparent 70%)`,
            transform: `translateX(${scan}px) skewX(-18deg)`,
          }}
        />
        <div
          style={{
            position: "absolute",
            top: 0,
            bottom: 0,
            width: 10,
            ...edgeStyle,
            background: module.accent,
            boxShadow: `0 0 24px ${module.accent}`,
          }}
        />
        <div
          style={{
            position: "absolute",
            inset: 28,
            display: "flex",
            flexDirection: "column",
            justifyContent: "space-between",
          }}
        >
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start" }}>
            <div>
              <div
                style={{
                  fontSize: 22,
                  letterSpacing: 6,
                  color: module.accent,
                  marginBottom: 16,
                }}
              >
                CORE MODULE 0{index + 1}
              </div>
              <div
                style={{
                  fontSize: 70,
                  fontWeight: 900,
                  letterSpacing: 2,
                  color: "white",
                  textShadow: `0 0 24px ${module.glow}`,
                }}
              >
                {module.title}
              </div>
            </div>
            <div
              style={{
                padding: "10px 18px",
                borderRadius: 999,
                border: `1px solid ${module.accent}99`,
                color: "#dff8ff",
                fontSize: 21,
                letterSpacing: 4,
              }}
            >
              LIVE EXECUTION
            </div>
          </div>
          <div style={{ display: "flex", gap: 18, alignItems: "center", flexWrap: "wrap" }}>
            {module.steps.map((step, stepIndex) => {
              const active = local > stepIndex * 4;
              return (
                <div key={step} style={{ display: "flex", alignItems: "center", gap: 18 }}>
                  <div
                    style={{
                      minWidth: 118,
                      padding: "14px 18px",
                      borderRadius: 18,
                      textAlign: "center",
                      color: active ? "#07111b" : "#dbeeff",
                      background: active ? module.accent : "rgba(255,255,255,0.06)",
                      boxShadow: active ? `0 0 26px ${module.glow}` : "none",
                      fontSize: 27,
                      fontWeight: 800,
                      letterSpacing: 2,
                    }}
                  >
                    {step}
                  </div>
                  {stepIndex < module.steps.length - 1 ? (
                    <div
                      style={{
                        width: 42,
                        height: 4,
                        borderRadius: 999,
                        background: active ? module.accent : "rgba(255,255,255,0.12)",
                      }}
                    />
                  ) : null}
                </div>
              );
            })}
          </div>
          <div
            style={{
              display: "grid",
              gridTemplateColumns: "1.2fr 0.8fr",
              gap: 18,
              alignItems: "center",
            }}
          >
            <div
              style={{
                color: "rgba(218,240,255,0.8)",
                fontSize: 28,
                letterSpacing: 2,
              }}
            >
              STATUS // orchestration, traceability, event sync, decision loop
            </div>
            <div
              style={{
                justifySelf: "end",
                fontSize: 26,
                color: module.accent,
                letterSpacing: 5,
              }}
            >
              STREAM ACTIVE
            </div>
          </div>
        </div>
      </div>
    </AbsoluteFill>
  );
};

const CentralHub: FC = () => {
  const frame = useCurrentFrame();
  const reveal = clampText(frame, 210, 250);
  const ringSpin = frame * 1.8;
  const pulse = 0.92 + Math.sin(frame / 6) * 0.05;

  return (
    <AbsoluteFill style={{ justifyContent: "center", alignItems: "center", opacity: reveal }}>
      <div
        style={{
          position: "relative",
          width: 940,
          height: 940,
          transform: `scale(${pulse})`,
        }}
      >
        <div
          style={{
            position: "absolute",
            inset: 120,
            borderRadius: "50%",
            border: "2px solid rgba(0,229,255,0.7)",
            boxShadow: "0 0 70px rgba(0,229,255,0.25)",
            transform: `rotate(${ringSpin}deg)`,
          }}
        />
        <div
          style={{
            position: "absolute",
            inset: 170,
            borderRadius: "50%",
            border: "2px dashed rgba(255,77,109,0.65)",
            transform: `rotate(${-ringSpin * 0.8}deg)`,
          }}
        />
        <div
          style={{
            position: "absolute",
            inset: 240,
            borderRadius: "50%",
            background:
              "radial-gradient(circle, rgba(0,229,255,0.36) 0%, rgba(114,241,184,0.18) 35%, rgba(4,8,24,0.95) 75%)",
            border: "1px solid rgba(255,255,255,0.12)",
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
            justifyContent: "center",
            textAlign: "center",
            boxShadow:
              "0 0 90px rgba(0,229,255,0.28), inset 0 0 70px rgba(0,0,0,0.4)",
          }}
        >
          <div className="eyebrow">UNIFIED ORCHESTRATION</div>
          <div
            style={{
              fontSize: 88,
              fontWeight: 900,
              lineHeight: 1,
              color: "white",
              letterSpacing: 3,
              textShadow: "0 0 24px rgba(255,255,255,0.2)",
            }}
          >
            ERP FLOW
          </div>
          <div
            style={{
              fontSize: 40,
              marginTop: 24,
              color: "rgba(220,242,255,0.88)",
              letterSpacing: 7,
            }}
          >
            AI SYNC. DATA FIRE. EXECUTE.
          </div>
        </div>
        {modules.map((module, index) => {
          const angle = (Math.PI * 2 * index) / modules.length - Math.PI / 2;
          const radius = 380;
          const x = 470 + Math.cos(angle) * radius;
          const y = 470 + Math.sin(angle) * radius;
          return (
            <div
              key={module.title}
              style={{
                position: "absolute",
                left: x - 112,
                top: y - 42,
                width: 224,
                padding: "16px 18px",
                borderRadius: 18,
                color: "white",
                background: "rgba(5,12,24,0.78)",
                border: `1px solid ${module.accent}99`,
                boxShadow: `0 0 24px ${module.glow}`,
                textAlign: "center",
                fontSize: 24,
                fontWeight: 700,
                letterSpacing: 2,
              }}
            >
              {module.title}
            </div>
          );
        })}
      </div>
    </AbsoluteFill>
  );
};

const FinalLockup: FC = () => {
  const frame = useCurrentFrame();
  const reveal = clampText(frame, 255, 286);
  const streak = interpolate(frame, [255, 300], [-900, 1800], {
    extrapolateLeft: "clamp",
    extrapolateRight: "clamp",
  });

  return (
    <AbsoluteFill
      style={{
        justifyContent: "flex-end",
        padding: "0 110px 80px 110px",
        opacity: reveal,
      }}
    >
      <div
        style={{
          position: "absolute",
          top: 120,
          left: streak,
          width: 360,
          height: 8,
          borderRadius: 999,
          background: "linear-gradient(90deg, transparent, #ffffff, transparent)",
          boxShadow: "0 0 30px rgba(255,255,255,0.75)",
          transform: "skewX(-22deg)",
        }}
      />
      <div
        style={{
          padding: "30px 36px",
          borderRadius: 28,
          background: "rgba(3,8,18,0.72)",
          border: "1px solid rgba(255,255,255,0.12)",
          backdropFilter: "blur(12px)",
          width: 1260,
        }}
      >
        <div className="eyebrow">SHOWREEL CUT / 10 SEC</div>
        <div
          style={{
            fontSize: 72,
            fontWeight: 900,
            color: "white",
            lineHeight: 1.08,
            letterSpacing: 2,
            marginTop: 14,
          }}
        >
          從請購到入庫，從工單到收款
          <br />
          全模組同步爆發
        </div>
      </div>
    </AbsoluteFill>
  );
};

export const ErpFlowComposition: FC = () => {
  return (
    <AbsoluteFill style={{ fontFamily: "Arial, 'Microsoft JhengHei', sans-serif" }}>
      <BattlefieldBackground />
      <HeaderBlast />
      {modules.map((module, index) => (
        <ModuleCard key={module.title} module={module} start={42 + index * 28} index={index} />
      ))}
      <CentralHub />
      <FinalLockup />
    </AbsoluteFill>
  );
};
