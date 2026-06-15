import type { FC } from "react";
import "./index.css";
import { Composition } from "remotion";
import { ErpFlowComposition } from "./Composition";

export const RemotionRoot: FC = () => {
  return (
    <>
      <Composition
        id="ErpFlowBattle"
        component={ErpFlowComposition}
        durationInFrames={300}
        fps={30}
        width={1920}
        height={1080}
      />
    </>
  );
};
