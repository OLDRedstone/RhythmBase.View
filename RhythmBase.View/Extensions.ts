import {
    AssetManager,
    Color,
    Paint,
    Point,
    Rect,
    SliceInfo,
} from "../RhythmBase.View/Assets/AssetManager.ts";
import {
    EventInfos,
    EventType,
    SayReadyGetSetGoWords,
    Tab,
    WordInfos,
} from "../RhythmBase.View/Assets/Consts.ts";

// 重新导出 AssetManager 及其相关类型
export { AssetManager, Color, Paint, Point, Rect, SliceInfo } from "../RhythmBase.View/Assets/AssetManager.ts";

enum PatchStyle {
    Repeat,
    Strentch,
}
export class IconStyle {
    Enabled?: boolean;
    Active: boolean = false;
    Hover: boolean = false;
    Scale: number = 2;
}
export class Extensions {
    private static readonly lineHeight = 6;
    private static readonly charHeight = 8;
    private static readonly iconSize = 14;
    private static readonly evtag = "event_tag";
    private static readonly evbarea = "event_beat_area";
    private static readonly asm = new AssetManager(
        "./Assets",
    );
    private constructor() {
        AssetManager.initialize();
    }
    private static DrawSlice(
        canvas: CanvasRenderingContext2D,
        src: string,
        dest: Rect,
        scale?: number,
        style?: PatchStyle,
    ): void;
    private static DrawSlice(
        canvas: CanvasRenderingContext2D,
        src: string,
        dest: Rect,
        scale?: number,
        style?: PatchStyle,
        color?: Color,
    ): void;
    private static DrawSlice(
        canvas: CanvasRenderingContext2D,
        src: string,
        dest: Point,
        scale?: number,
        style?: PatchStyle,
    ): Rect;
    private static DrawSlice(
        canvas: CanvasRenderingContext2D,
        src: string,
        dest: Point,
        scale?: number,
        style?: PatchStyle,
        color?: Color,
    ): Rect;
    private static DrawSlice(
        canvas: CanvasRenderingContext2D,
        src: string,
        dest: Rect | Point,
        scale: number = 1,
        style: PatchStyle = PatchStyle.Strentch,
        color?: Color,
    ): Rect | void {
        const ctx = canvas;
        if (!ctx) return;

        if (!AssetManager.slices.has(src)) {
            return;
        }

        const info = AssetManager.slices.get(src)!;

        if (dest instanceof Point) {
            // DrawSlice with Point dest - returns Rect
            const destRect = new Rect(
                dest.x - info.pivot.x * scale,
                dest.y - info.pivot.y * scale,
                info.bounds.width * scale,
                info.bounds.height * scale,
            );

            if (color) {
                if (color.alpha === 0) {
                    return destRect;
                }
                this.drawSliceWithColor(ctx, info, destRect, color);
            } else {
                this.drawSliceNormal(ctx, info, destRect);
            }

            return destRect;
        } else {
            // DrawSlice with Rect dest - returns void
            if (info.isNinePatch) {
                this.DrawNinePatch(canvas, info, dest, color, scale, style);
            } else {
                if (color) {
                    this.drawSliceWithColor(ctx, info, dest, color);
                } else {
                    this.drawSliceNormal(ctx, info, dest);
                }
            }
        }
    }

    private static drawSliceNormal(
        ctx: CanvasRenderingContext2D,
        info: SliceInfo,
        destRect: Rect,
    ): void {
        const bitmap = AssetManager.assetFile;
        if (!bitmap) return;
        ctx.imageSmoothingEnabled = false;
        ctx.drawImage(
            bitmap as any,
            info.bounds.x,
            info.bounds.y,
            info.bounds.width,
            info.bounds.height,
            destRect.x,
            destRect.y,
            destRect.width,
            destRect.height,
        );
    }

    private static drawSliceWithColor(
        ctx: CanvasRenderingContext2D,
        info: SliceInfo,
        destRect: Rect,
        color: Color,
    ): void {
        const bitmap = AssetManager.assetFile;
        if (!bitmap) return;

        if (color.alpha === 0) {
            return;
        }

        const tr = color.red / 255;
        const tg = color.green / 255;
        const tb = color.blue / 255;
        const ta = color.alpha / 255;

        // 保存当前状�?
        ctx.save();

        // 创建临时canvas用于颜色处理
        const tempCanvas = typeof document !== "undefined" ? document.createElement("canvas") : ({} as any); tempCanvas.width = info.bounds.width; tempCanvas.height = info.bounds.height;
        const tempCtx = tempCanvas.getContext("2d");

        tempCtx.imageSmoothingEnabled = false;
        // 先绘制原图到临时画布
        tempCtx.drawImage(
            bitmap as any,
            info.bounds.x,
            info.bounds.y,
            info.bounds.width,
            info.bounds.height,
            0,
            0,
            info.bounds.width,
            info.bounds.height,
        );

        // 获取图像数据并应用颜色矩阵（灰度�?+ 着色）
        const imageData = tempCtx.getImageData(
            0,
            0,
            info.bounds.width,
            info.bounds.height,
        );
        const data = imageData.data;

        for (let i = 0; i < data.length; i += 4) {
            // 保持原始 alpha 通道
            const originalAlpha = data[i + 3];

            // 计算灰度�?
            const gray = data[i] * 0.2126 + data[i + 1] * 0.7152 +
                data[i + 2] * 0.0722;

            // 将灰度值映射到目标颜色，保持亮�?
            data[i] = gray * tr;
            data[i + 1] = gray * tg;
            data[i + 2] = gray * tb;
            data[i + 3] = originalAlpha * ta;
        }

        tempCtx.putImageData(imageData, 0, 0);

        // 绘制处理后的图像到目标画�?
        ctx.imageSmoothingEnabled = false;
        ctx.globalAlpha = 1.0;
        ctx.drawImage(
            tempCanvas as any,
            0,
            0,
            info.bounds.width,
            info.bounds.height,
            destRect.x,
            destRect.y,
            destRect.width,
            destRect.height,
        );

        ctx.restore();
    }

    private static DrawNinePatch(
        canvas: CanvasRenderingContext2D,
        info: SliceInfo,
        dest: Rect,
        color?: Color,
        scale: number = 1,
        style: PatchStyle = PatchStyle.Strentch,
    ): void {
        const ctx = canvas;
        if (!ctx) return;

        const bitmap = AssetManager.assetFile;
        if (!bitmap) return;

        const srcBounds = info.bounds;
        const destRect = dest;
        const centerBounds = info.center;

        // 源坐�?
        const sx0 = srcBounds.left;
        const sx3 = srcBounds.right;
        const sy0 = srcBounds.top;
        const sy3 = srcBounds.bottom;

        const sx1 = sx0 + centerBounds.left;
        const sx2 = sx0 + centerBounds.right;
        const sy1 = sy0 + centerBounds.top;
        const sy2 = sy0 + centerBounds.bottom;

        const swLeft = sx1 - sx0;
        const swCenter = sx2 - sx1;
        const swRight = sx3 - sx2;

        const shTop = sy1 - sy0;
        const shCenter = sy2 - sy1;
        const shBottom = sy3 - sy2;

        // 目标宽度
        const dwLeft = swLeft * scale;
        let dwRight = swRight * scale;
        let dwCenter = destRect.width - dwLeft - dwRight;
        if (dwCenter < 0) {
            const scaleX = destRect.width / Math.max(1, swLeft + swRight);
            dwRight = Math.max(0, destRect.width - Math.round(swLeft * scaleX));
            dwCenter = 0;
        }

        // 目标高度
        const dhTop = shTop * scale;
        let dhBottom = shBottom * scale;
        let dhCenter = destRect.height - dhTop - dhBottom;
        if (dhCenter < 0) {
            const scaleY = destRect.height / Math.max(1, shTop + shBottom);
            dhBottom = Math.max(
                0,
                destRect.height - Math.round(shTop * scaleY),
            );
            dhCenter = 0;
        }

        const srcXs = [sx0, sx1, sx2, sx3];
        const srcYs = [sy0, sy1, sy2, sy3];

        const dx0 = destRect.left;
        const dx1 = dx0 + dwLeft;
        const dx2 = dx1 + dwCenter;
        const dx3 = destRect.right;

        const dy0 = destRect.top;
        const dy1 = dy0 + dhTop;
        const dy2 = dy1 + dhCenter;
        const dy3 = destRect.bottom;

        const dstXs = [dx0, dx1, dx2, dx3];
        const dstYs = [dy0, dy1, dy2, dy3];

        // 9宫格循环绘制
        for (let row = 0; row < 3; row++) {
            for (let col = 0; col < 3; col++) {
                const sLeft = srcXs[col];
                const sTop = srcYs[row];
                const sRight = srcXs[col + 1];
                const sBottom = srcYs[row + 1];
                const sW = sRight - sLeft;
                const sH = sBottom - sTop;
                if (sW <= 0 || sH <= 0) continue;

                const dLeft = dstXs[col];
                const dTop = dstYs[row];
                const dRight = dstXs[col + 1];
                const dBottom = dstYs[row + 1];
                const dW = dRight - dLeft;
                const dH = dBottom - dTop;
                if (dW <= 0 || dH <= 0) continue;

                if (style === PatchStyle.Repeat) {
                    for (let y = dTop; y < dBottom; y += sH * scale) {
                        const th = Math.min(sH, (dBottom - y) / scale);
                        for (let x = dLeft; x < dRight; x += sW * scale) {
                            const tw = Math.min(sW, (dRight - x) / scale);
                            var sRect = new Rect(sLeft, sTop, tw, th);
                            var dRect = new Rect(x, y, tw * scale, th * scale);
                            if (color && color.alpha > 0) {
                                this.drawSliceWithColor(
                                    ctx,
                                    {
                                        bounds: sRect,
                                        pivot: new Point(0, 0),
                                        isNinePatch: false,
                                        center: new Rect(),
                                        scale: 1,
                                    } as any,
                                    dRect,
                                    color,
                                );
                            } else {
                                this.drawSliceNormal(
                                    ctx,
                                    {
                                        bounds: sRect,
                                        pivot: new Point(0, 0),
                                        isNinePatch: false,
                                        center: new Rect(),
                                        scale: 1,
                                    } as any,
                                    dRect,
                                );
                            }
                        }
                    }
                } else {
                    // 拉伸模式或非中心区域
                    if (color && color.alpha > 0) {
                        // 使用着色绘�?
                        this.drawSliceWithColor(
                            ctx,
                            {
                                bounds: new Rect(sLeft, sTop, sW, sH),
                                pivot: new Point(0, 0),
                                isNinePatch: false,
                                center: new Rect(),
                                scale: 1,
                            } as any,
                            new Rect(dLeft, dTop, dW, dH),
                            color,
                        );
                    } else {
                        // 直接绘制
                        ctx.imageSmoothingEnabled = false;
                        ctx.drawImage(
                            bitmap as any,
                            sLeft,
                            sTop,
                            sW,
                            sH,
                            dLeft,
                            dTop,
                            dW,
                            dH,
                        );
                    }
                }
            }
        }
    }
    public static MeasureRDFontText(
        canvas: CanvasRenderingContext2D,
        text: string,
        scale: number = 1,
    ): number {
        let len = 0;
        for (let i = 0; i < text.length; i++) {
            const c = text.charAt(i);
            if (c === "\n") {
                len = 0;
            } else {
                const info = AssetManager.slices.get(
                    `char_${c.charCodeAt(0).toString(16).padStart(4, "0")}`,
                );
                if (info) {
                    len += info.bounds.width * scale;
                }
            }
        }
        return len;
    }
    public static DrawRDFontText(
        canvas: CanvasRenderingContext2D,
        text: string,
        dest: Point,
        color: Color,
        scale: number = 1,
    ): Rect {
        const start = dest.clone();
        const result = new Rect();
        start.y -= this.lineHeight * scale;
        for (let i = 0; i < text.length; i++) {
            const c = text.charAt(i);
            if (c === "\n") {
                start.x = dest.x;
                start.y += this.lineHeight * scale;
                continue;
            } else {
                const charcode = c.charCodeAt(0).toString(16).padStart(4, "0");
                const area = this.DrawSlice(
                    canvas,
                    `char_${charcode}`,
                    start,
                    scale,
                    PatchStyle.Strentch,
                    color,
                );
                result.union(area);
                start.x += area.width * scale;
            }
        }
        return result;
    }
    public static DrawEventIcon(
        canvas: CanvasRenderingContext2D,
        evt: any,
        dest: Point,
        style: IconStyle,
    ): Rect {
        let destRect: Rect = new Rect();
        const key = `event_${evt.type}`;
        const evttype: string = evt.type;
        const info = AssetManager.slices.get(key) ??
            AssetManager.slices.get("event_Unknown") ?? (() => {
                throw new Error("information not found.");
            });
        if (!(info instanceof SliceInfo)) {
            return new Rect();
        }
        const pulse = "event_beat_pulse";
        const hit = "event_beat_hit";
        const hitf = "event_beat_hit_freeze";
        const hitb = "event_beat_hit_burn";
        const beatcross = "event_beat_cross";
        const beatx = "event_beat_x";
        const beatsynco = "event_beat_synco";
        const beatline = "event_beat_line";

        const beatcolor = new Color(0xff60e345);

        const evtInfo = EventInfos[evt.type as EventType];
        evt.tab = evt.tab ?? evtInfo.defaultTab;
        switch (evt.type as EventType) {
            case EventType.AddClassicBeat:
                {
                    const tick = (evt.tick ?? 1) as number;
                    const swing = (evt.swing === 0 ? 1 : evt.swing) as number;
                    const hold = (evt.hold ?? 0) as number;
                    const syncoswing = (evt.syncoSwing ?? 0) as number;
                    const syncobeat = (evt.syncoBeat ?? -1) as number;
                    const classicbeatlength = (evt.length ?? 7) as number;
                    destRect = new Rect(
                        dest.x,
                        dest.y,
                        this.iconSize * style.Scale *
                            (tick *
                                (classicbeatlength - 1 - (syncoswing ?? 0))),
                        this.iconSize * style.Scale,
                    );
                    if (hold > 0) {
                        this.DrawSlice(
                            canvas,
                            this.evbarea,
                            new Rect(
                                dest.x + destRect.width,
                                dest.y,
                                this.iconSize * style.Scale * hold,
                                this.iconSize * style.Scale,
                            ),
                            style.Scale,
                            PatchStyle.Repeat,
                            new Color(style.Active ? 0xffd046f3 : 0xff7e3990),
                        );
                    }
                    this.DrawBack(
                        canvas,
                        destRect.clone(),
                        this.WithState(
                            this.ColorOf(evt.tab),
                            style.Active,
                            style.Enabled ?? evt.active ?? true,
                        ),
                        style,
                    );
                    if (style.Hover) {
                        for (let i = 0; i < classicbeatlength - 1; i++) {
                            this.DrawSlice(
                                canvas,
                                pulse,
                                new Point(
                                    dest.x +
                                        this.iconSize * style.Scale *
                                            (tick *
                                                (i +
                                                    i % 2 * (1 - (swing ?? 1)) -
                                                    (i <= (syncobeat ?? -1)
                                                        ? 0
                                                        : (syncoswing ?? 0)))),
                                    dest.y,
                                ),
                                style.Scale,
                            );
                        }
                    }
                    this.DrawSlice(
                        canvas,
                        hit,
                        new Point(
                            dest.x +
                                style.Scale *
                                    (this.iconSize *
                                            (tick *
                                                (classicbeatlength - 1 -
                                                    (syncoswing ?? 0))) -
                                        1),
                            dest.y,
                        ),
                        style.Scale,
                    );
                }
                break;
            case EventType.AddFreeTimeBeat:
                {
                    const hold = (evt.hold ?? 0) -
                        info.bounds.width / this.iconSize;
                    destRect = new Rect(
                        dest.x,
                        dest.y,
                        info.bounds.width * style.Scale,
                        info.bounds.height * style.Scale,
                    );
                    this.DrawBack(
                        canvas,
                        destRect.clone(),
                        this.WithState(
                            beatcolor.clone(),
                            style.Active,
                            style.Enabled ?? evt.active ?? true,
                        ),
                        style,
                    );
                    this.DrawRDFontText(
                        canvas,
                        ((evt.pulse ?? 0) + 1).toString(),
                        new Point(
                            dest.x + 1.5 * style.Scale,
                            dest.y + 10 * style.Scale,
                        ),
                        new Color(0xffffffff),
                        style.Scale,
                    );
                    if ((evt.pulse ?? 0) == 6) {
                        this.DrawSlice(
                            canvas,
                            hit,
                            new Point(
                                dest.x,
                                dest.y,
                            ),
                            style.Scale,
                        );
                    }
                    if (hold > 0) {
                        this.DrawSlice(
                            canvas,
                            this.evbarea,
                            new Rect(
                                dest.x + destRect.width,
                                dest.y,
                                this.iconSize * style.Scale * hold,
                                info.bounds.height * style.Scale,
                            ),
                            style.Scale,
                            PatchStyle.Repeat,
                            new Color(style.Active ? 0xff60e345 : 0xff2f7122),
                        );
                    }
                }
                break;
            case EventType.AddOneshotBeat:
                {
                    const tick = (evt.tick ?? 1) as number;
                    const interval = (evt.interval ?? 1) as number;
                    const loop = Math.floor(evt.loops ?? 0) as number;
                    const subdiv = (evt.subdivisions ?? 1) as number;
                    const freezeBurnMode = evt.freezeBurnMode ?? "Wave";
                    const delay = freezeBurnMode === "Freezeshot"
                        ? (evt.delay ?? 0)
                        : 0;

                    destRect = new Rect(
                        dest.x,
                        dest.y,
                        this.iconSize * style.Scale *
                            (loop * interval + tick + delay),
                        this.iconSize * style.Scale,
                    );

                    this.DrawBack(
                        canvas,
                        destRect.clone(),
                        this.WithState(
                            this.ColorOf(evt.tab),
                            style.Active,
                            style.Enabled ?? evt.active ?? true,
                        ),
                        style,
                    );

                    const subdivwidth = this.iconSize * style.Scale *
                        (subdiv - 1) / subdiv * tick;
                    if (subdiv > 1) {
                        this.DrawSlice(
                            canvas,
                            this.evbarea,
                            new Rect(
                                dest.x + destRect.width,
                                dest.y,
                                subdivwidth,
                                info.bounds.height * style.Scale,
                            ),
                            style.Scale,
                            PatchStyle.Repeat,
                            new Color(0xff13B021),
                        );
                    }

                    if (evt.skipshot ?? false) {
                        this.DrawSlice(
                            canvas,
                            this.evbarea,
                            new Rect(
                                dest.x + destRect.width + subdivwidth,
                                dest.y,
                                style.Scale * this.iconSize * interval -
                                    subdivwidth,
                                info.bounds.height * style.Scale,
                            ),
                            style.Scale,
                            PatchStyle.Repeat,
                            new Color(0xffc53b3b),
                        );
                    }

                    if (style.Hover) {
                        for (let l = 0; l <= loop; l++) {
                            for (let i = 0; i < subdiv; i++) {
                                this.DrawSlice(
                                    canvas,
                                    pulse,
                                    new Point(
                                        dest.x +
                                            this.iconSize * style.Scale *
                                                (l * interval +
                                                    i * tick / subdiv),
                                        dest.y,
                                    ),
                                    style.Scale,
                                );
                            }
                        }
                    }

                    for (let l = 0; l <= loop; l++) {
                        for (let i = 0; i < subdiv; i++) {
                            this.DrawSlice(
                                canvas,
                                hit,
                                new Point(
                                    dest.x +
                                        style.Scale *
                                            (this.iconSize *
                                                    (l * interval + delay +
                                                        tick +
                                                        (i * tick / subdiv)) -
                                                1),
                                    dest.y,
                                ),
                                style.Scale,
                            );
                        }
                    }

                    const off = interval - tick;
                    if (freezeBurnMode !== "Wave") {
                        let posx = dest.x - off * style.Scale * this.iconSize;
                        this.DrawSlice(
                            canvas,
                            beatcross,
                            new Point(posx, dest.y),
                            style.Scale,
                        );

                        switch (freezeBurnMode) {
                            case "Freezeshot":
                                posx += delay * style.Scale * this.iconSize;
                                this.DrawSlice(
                                    canvas,
                                    beatcross,
                                    new Point(posx, dest.y),
                                    style.Scale,
                                );
                                for (let l = 0; l <= loop; l++) {
                                    this.DrawSlice(
                                        canvas,
                                        hitf,
                                        new Point(
                                            dest.x +
                                                style.Scale *
                                                    (this.iconSize *
                                                            (l * interval +
                                                                tick) - 1),
                                            dest.y,
                                        ),
                                        style.Scale,
                                    );
                                }
                                break;
                            case "Burnshot":
                                posx -= tick * style.Scale * this.iconSize;
                                this.DrawSlice(
                                    canvas,
                                    beatcross,
                                    new Point(posx, dest.y),
                                    style.Scale,
                                );
                                for (let l = 0; l <= loop; l++) {
                                    this.DrawSlice(
                                        canvas,
                                        hitb,
                                        new Point(
                                            dest.x +
                                                style.Scale *
                                                    (this.iconSize *
                                                            (l * interval +
                                                                tick) - 1),
                                            dest.y,
                                        ),
                                        style.Scale,
                                    );
                                }
                                break;
                            default:
                                break;
                        }
                    }

                    if (style.Active && style.Hover) {
                        this.DrawSlice(
                            canvas,
                            "event_beat_loop",
                            new Point(destRect.right, destRect.top),
                            style.Scale,
                        );
                    }
                }
                break;
            case EventType.Comment:
                {
                    const commentColor = new Color(evt.color);
                    console.log(evt.color, commentColor);
                    destRect = new Rect(
                        dest.x,
                        dest.y,
                        info.bounds.width * style.Scale,
                        info.bounds.height * style.Scale,
                    );
                    this.DrawBack(
                        canvas,
                        destRect.clone(),
                        this.WithState(
                            commentColor,
                            style.Active,
                            style.Enabled ?? evt.active ?? true,
                        ),
                        style,
                    );
                    this.DrawSlice(
                        canvas,
                        key,
                        dest,
                        style.Scale,
                    );
                }
                break;
            case EventType.DesktopColor:
                {
                    destRect = new Rect(
                        dest.x,
                        dest.y,
                        info.bounds.width * style.Scale,
                        info.bounds.height * 4 * style.Scale,
                    );
                    this.DrawBack(
                        canvas,
                        destRect.clone(),
                        this.WithState(
                            this.ColorOf(evt.tab),
                            style.Active,
                            style.Enabled ?? evt.active ?? true,
                        ),
                        style,
                    );
                    const endColor = new Color(evt.endColor);
                    this.DrawSlice(
                        canvas,
                        `${key}_0`,
                        new Point(
                            dest.x,
                            dest.y + info.bounds.height * style.Scale,
                        ),
                        style.Scale,
                        PatchStyle.Strentch,
                        endColor,
                    );
                    this.DrawSlice(
                        canvas,
                        key,
                        new Point(
                            dest.x,
                            dest.y + info.bounds.height * style.Scale,
                        ),
                        style.Scale,
                    );
                }
                break;
            case EventType.PulseFreeTimeBeat:
                {
                    const hold = (evt.hold ?? 0) -
                        info.bounds.width / this.iconSize;
                    destRect = new Rect(
                        dest.x,
                        dest.y,
                        info.bounds.width * style.Scale,
                        info.bounds.height * style.Scale,
                    );
                    this.DrawBack(
                        canvas,
                        destRect.clone(),
                        this.WithState(
                            beatcolor.clone(),
                            style.Active,
                            style.Enabled ?? evt.active ?? true,
                        ),
                        style,
                    );

                    let actionText: string;
                    switch (evt.action) {
                        case "Increment":
                            actionText = ">";
                            break;
                        case "Decrement":
                            actionText = "<";
                            break;
                        case "Remove":
                            actionText = "x";
                            break;
                        case "Custom":
                        default:
                            actionText = ((evt.customPulse ?? 0) + 1)
                                .toString();
                            break;
                    }

                    this.DrawRDFontText(
                        canvas,
                        actionText,
                        new Point(
                            dest.x + 1.5 * style.Scale,
                            dest.y + 8 * style.Scale,
                        ),
                        new Color(0xffffffff),
                        style.Scale,
                    );

                    if (
                        evt.action === "Custom" && (evt.customPulse ?? 0) === 7
                    ) {
                        this.DrawSlice(
                            canvas,
                            hit,
                            new Rect(
                                dest.x - 2 * style.Scale,
                                dest.y,
                                5 * style.Scale,
                                info.bounds.height * style.Scale,
                            ),
                            style.Scale,
                            PatchStyle.Strentch,
                        );
                    }

                    if (hold > 0) {
                        this.DrawSlice(
                            canvas,
                            this.evbarea,
                            new Rect(
                                dest.x + destRect.width,
                                dest.y,
                                this.iconSize * style.Scale * hold,
                                info.bounds.height * style.Scale,
                            ),
                            style.Scale,
                            PatchStyle.Repeat,
                            new Color(style.Active ? 0xffd046f3 : 0xff7e3990),
                        );
                    }
                }
                break;
            case EventType.ReorderRooms:
                {
                    destRect = new Rect(
                        dest.x,
                        dest.y,
                        this.iconSize * style.Scale,
                        this.iconSize * 4 * style.Scale,
                    );
                    this.DrawBack(
                        canvas,
                        destRect.clone(),
                        this.WithState(
                            this.ColorOf(evt.tab),
                            style.Active,
                            style.Enabled ?? evt.active ?? true,
                        ),
                        style,
                    );

                    let currentDest = dest.clone();
                    if (evt.order && Array.isArray(evt.order)) {
                        for (const r of evt.order) {
                            this.DrawSlice(
                                canvas,
                                `${key}_${r}`,
                                currentDest.clone(),
                                style.Scale,
                            );
                            currentDest.y += this.iconSize * style.Scale;
                        }
                    }
                }
                break;
            case EventType.ReorderWindows:
                {
                    destRect = new Rect(
                        dest.x,
                        dest.y,
                        this.iconSize * style.Scale,
                        this.iconSize * 4 * style.Scale,
                    );
                    this.DrawBack(
                        canvas,
                        destRect.clone(),
                        this.WithState(
                            this.ColorOf(evt.tab),
                            style.Active,
                            style.Enabled ?? evt.active ?? true,
                        ),
                        style,
                    );

                    let currentDest = dest.clone();
                    if (evt.order && Array.isArray(evt.order)) {
                        for (const r of evt.order) {
                            this.DrawSlice(
                                canvas,
                                `${key}_${r}`,
                                currentDest.clone(),
                                style.Scale,
                            );
                            currentDest.y += this.iconSize * style.Scale;
                        }
                    }
                }
                break;
            case EventType.SayReadyGetSetGo:
                {
                    const wordInfo =
                        WordInfos[evt.phraseToSay as SayReadyGetSetGoWords];
                    const len = wordInfo.length * evt.tick + 1;
                    destRect = new Rect(
                        dest.x,
                        dest.y,
                        len * this.iconSize * style.Scale,
                        this.iconSize * style.Scale,
                    );
                    this.DrawBack(
                        canvas,
                        destRect.clone(),
                        this.WithState(
                            this.ColorOf(evt.tab),
                            style.Active,
                            style.Enabled ?? evt.active ?? true,
                        ),
                        style,
                    );
                    const stringToJion = wordInfo.phrase.split(" ");
                    let stringToDraw = [stringToJion[0]];
                    let lw = this.MeasureRDFontText(canvas, stringToJion[0]);
                    const sw = this.MeasureRDFontText(canvas, " ");
                    for (let i = 1; i < stringToJion.length; i++) {
                        const part = stringToJion[i];
                        const w = this.MeasureRDFontText(canvas, part);
                        if (
                            lw + sw + w >
                                (len * this.iconSize - 2) * style.Scale
                        ) {
                            lw = w;
                            stringToDraw.push(part);
                        } else {
                            lw += sw + w;
                            stringToDraw[stringToDraw.length - 1] += " " + part;
                        }
                    }
                    stringToDraw.splice(3);
                    const c = stringToDraw.length;
                    const top = dest.y + (this.iconSize - this.charHeight * c / 2) * style.Scale / 2;
                    for(let i=0;i<c;i++){
                    const line = stringToDraw[i];
                    const p = new Point(
                        dest.x + (len * this.iconSize * style.Scale - this.MeasureRDFontText(canvas, line, style.Scale / 2)) / 2,
                        top + (i * this.charHeight + this.lineHeight) * style.Scale / 2);
                    this.DrawRDFontText(canvas, line, p, new Color(0xffffffff), style.Scale / 2);
                    }
                }
                break;
            case EventType.SetRowXs:
                {
                    destRect = new Rect(
                        dest.x,
                        dest.y,
                        info.bounds.width * style.Scale,
                        info.bounds.height * style.Scale,
                    );
                    this.DrawBack(
                        canvas,
                        destRect.clone(),
                        this.WithState(
                            this.ColorOf(evt.tab),
                            style.Active,
                            style.Enabled ?? evt.active ?? true,
                        ),
                        style,
                    );

                    const width = info.bounds.width * style.Scale / 6;
                    const beatxInfo = AssetManager.slices.get(beatx);
                    if (!beatxInfo) break;

                    const iconwidth = beatxInfo.bounds.width;
                    const s = width / iconwidth;
                    let left = 0;
                    const top = this.iconSize * style.Scale / 2 -
                        beatxInfo.bounds.height * s / 2;

                    if (evt.pattern) {
                        const pattern = evt.pattern as string;
                        for (const p of pattern) {
                            const patternKey = p === "x" ? beatx : beatline;
                            this.DrawSlice(
                                canvas,
                                patternKey,
                                new Rect(
                                    dest.x + left,
                                    dest.y + top,
                                    width,
                                    beatxInfo.bounds.height * s,
                                ),
                                style.Scale,
                                PatchStyle.Strentch,
                            );
                            left += width;
                        }
                    }

                    if ((evt.syncoBeat ?? -1) >= 0) {
                        this.DrawSlice(
                            canvas,
                            beatsynco,
                            new Rect(
                                dest.x + width * (evt.syncoBeat ?? 0),
                                dest.y + top,
                                width,
                                beatxInfo.bounds.height * s,
                            ),
                            style.Scale,
                            PatchStyle.Strentch,
                        );
                    }
                }
                break;
            case EventType.ShowRooms:
                {
                    destRect = new Rect(
                        dest.x,
                        dest.y,
                        this.iconSize * style.Scale,
                        this.iconSize * 4 * style.Scale,
                    );
                    this.DrawBack(
                        canvas,
                        destRect.clone(),
                        this.WithState(
                            this.ColorOf(evt.tab),
                            style.Active,
                            style.Enabled ?? evt.active ?? true,
                        ),
                        style,
                    );

                    if (evt.rooms && Array.isArray(evt.rooms)) {
                        for (let i = 0; i < 4; i++) {
                            const roomState = evt.rooms[i] ? "1" : "0";
                            this.DrawSlice(
                                canvas,
                                `${key}_${roomState}`,
                                new Point(
                                    dest.x,
                                    dest.y + i * this.iconSize * style.Scale,
                                ),
                                style.Scale / 2,
                            );
                        }
                    }
                }
                break;
            default:
                destRect = new Rect(
                    dest.x,
                    dest.y,
                    info.bounds.width * style.Scale,
                    info.bounds.height * style.Scale,
                );
                this.DrawBack(
                    canvas,
                    destRect.clone(),
                    this.WithState(
                        this.ColorOf(evt.tab),
                        style.Active,
                        style.Enabled ?? evt.active ?? true,
                    ),
                    style,
                );
                switch (evt.type as EventType) {
                    case EventType.CustomFlash:
                        {
                            this.DrawSlice(
                                canvas,
                                `${key}_0`,
                                dest,
                                style.Scale,
                                PatchStyle.Strentch,
                                new Color(evt.startColor ?? 0x00000000),
                            );
                            this.DrawSlice(
                                canvas,
                                `${key}_1`,
                                dest,
                                style.Scale,
                                PatchStyle.Strentch,
                                new Color(evt.endColor ?? 0x00000000),
                            );
                        }
                        break;
                    case EventType.FlipScreen:
                        {
                            let suffix = "";
                            const flipX = evt.flipX ?? false;
                            const flipY = evt.flipY ?? false;
                            if (flipX && flipY) {
                                suffix = "_2";
                            } else if (flipX && !flipY) {
                                suffix = "_1";
                            } else if (!flipX && flipY) {
                                suffix = "_0";
                            }
                            this.DrawSlice(
                                canvas,
                                key + suffix,
                                dest,
                                style.Scale,
                            );
                        }
                        break;
                    case EventType.FloatingText:
                        {
                            this.DrawSlice(
                                canvas,
                                `${key}_0`,
                                dest,
                                style.Scale,
                                PatchStyle.Strentch,
                                new Color(evt.color ?? 0x00000000),
                            );
                            this.DrawSlice(
                                canvas,
                                `${key}_1`,
                                dest,
                                style.Scale,
                                PatchStyle.Strentch,
                                new Color(evt.outlineColor ?? 0x00000000),
                            );
                        }
                        break;
                    case EventType.MoveRoom:
                        {
                            canvas.save();
                            const degree = evt.angle ?? 0;
                            const scaleWidth = evt.scale?.width ?? 0;
                            const scaleHeight = evt.scale?.height ?? 0;

                            if (scaleWidth === 0 || scaleHeight === 0) {
                                canvas.restore();
                                break;
                            }

                            const pleft = (evt.pivot?.x ?? 0) / 100;
                            const ptop = 1 - (evt.pivot?.y ?? 0) / 100;

                            const uniform = Math.sqrt(
                                scaleWidth * scaleWidth +
                                    scaleHeight * scaleHeight,
                            );
                            const w = scaleWidth / uniform;
                            const h = scaleHeight / uniform;

                            canvas.translate(
                                dest.x + destRect.width / 2,
                                dest.y + destRect.height / 2,
                            );
                            canvas.rotate((-degree * Math.PI) / 180);
                            canvas.scale(w, h);
                            this.DrawSlice(
                                canvas,
                                key,
                                new Point(
                                    -destRect.width / 2,
                                    -destRect.height / 2,
                                ),
                                style.Scale,
                            );

                            if (
                                !(pleft < 0 || pleft > 1 || ptop < 0 ||
                                    ptop > 1)
                            ) {
                                canvas.fillStyle = "rgb(255, 0, 0)";
                                canvas.fillRect(
                                    (pleft - 0.5) * destRect.width - 1,
                                    (ptop - 0.5) * destRect.height - 1,
                                    2,
                                    2,
                                );
                            }
                            canvas.restore();
                        }
                        break;
                    case EventType.PaintHands:
                        {
                            this.DrawSlice(
                                canvas,
                                key,
                                dest,
                                style.Scale,
                                PatchStyle.Strentch,
                                new Color(evt.tintColor ?? 0x00000000),
                            );
                            const border = evt.border ?? "None";
                            if (border === "Outline") {
                                this.DrawSlice(
                                    canvas,
                                    `${key}_0`,
                                    dest,
                                    style.Scale,
                                    PatchStyle.Strentch,
                                    new Color(
                                        evt.borderColor ?? 0x00000000,
                                    ),
                                );
                            } else if (border === "Glow") {
                                this.DrawSlice(
                                    canvas,
                                    `${key}_1`,
                                    dest,
                                    style.Scale,
                                    PatchStyle.Strentch,
                                    new Color(
                                        evt.borderColor ?? 0x00000000,
                                    ),
                                );
                            }
                        }
                        break;
                    case EventType.SetBackgroundColor:
                        {
                            this.DrawSlice(
                                canvas,
                                key,
                                dest,
                                style.Scale,
                            );
                            const bgType = evt.backgroundType ?? "Color";
                            if (bgType === "Color") {
                                this.DrawSlice(
                                    canvas,
                                    `${key}_0`,
                                    dest,
                                    style.Scale,
                                    PatchStyle.Strentch,
                                    new Color(evt.color ?? 0x00000000),
                                );
                            } else if (bgType === "Image") {
                                this.DrawSlice(
                                    canvas,
                                    `${key}_0`,
                                    dest,
                                    style.Scale,
                                    PatchStyle.Strentch,
                                    new Color(0xffffffff),
                                );
                            }
                        }
                        break;
                    case EventType.SetCrotchetsPerBar:
                        {
                            const cpb = evt.crotchetsPerBar ?? 4;
                            this.DrawSlice(
                                canvas,
                                key,
                                dest,
                                style.Scale,
                            );
                            this.DrawRDFontText(
                                canvas,
                                cpb > 9 ? "-" : cpb.toString(),
                                new Point(
                                    dest.x + 2 * style.Scale,
                                    dest.y + 7 * style.Scale,
                                ),
                                new Color(0xff000000),
                                style.Scale,
                            );
                            this.DrawRDFontText(
                                canvas,
                                "4",
                                new Point(
                                    dest.x + 8 * style.Scale,
                                    dest.y + 12 * style.Scale,
                                ),
                                new Color(0xff000000),
                                style.Scale,
                            );
                        }
                        break;
                    case EventType.SetForeground:
                        {
                            this.DrawSlice(
                                canvas,
                                key,
                                dest,
                                style.Scale,
                            );
                        }
                        break;
                    case EventType.Tint:
                        {
                            this.DrawSlice(
                                canvas,
                                key,
                                dest,
                                style.Scale,
                                PatchStyle.Strentch,
                                new Color(evt.tintColor ?? 0x00000000),
                            );
                            const border = evt.border ?? "None";
                            if (border === "Outline") {
                                this.DrawSlice(
                                    canvas,
                                    `${key}_0`,
                                    dest,
                                    style.Scale,
                                    PatchStyle.Strentch,
                                    new Color(
                                        evt.borderColor ?? 0x00000000,
                                    ),
                                );
                            } else if (border === "Glow") {
                                this.DrawSlice(
                                    canvas,
                                    `${key}_1`,
                                    dest,
                                    style.Scale,
                                    PatchStyle.Strentch,
                                    new Color(
                                        evt.borderColor ?? 0x00000000,
                                    ),
                                );
                            }
                        }
                        break;
                    case EventType.TintRows:
                        {
                            this.DrawSlice(
                                canvas,
                                key,
                                dest,
                                style.Scale,
                                PatchStyle.Strentch,
                                new Color(evt.tintColor ?? 0x00000000),
                            );
                            const border = evt.border ?? "None";
                            if (border === "Outline") {
                                this.DrawSlice(
                                    canvas,
                                    `${key}_0`,
                                    dest,
                                    style.Scale,
                                    PatchStyle.Strentch,
                                    new Color(
                                        evt.borderColor ?? 0x00000000,
                                    ),
                                );
                            } else if (border === "Glow") {
                                this.DrawSlice(
                                    canvas,
                                    `${key}_1`,
                                    dest,
                                    style.Scale,
                                    PatchStyle.Strentch,
                                    new Color(
                                        evt.borderColor ?? 0x00000000,
                                    ),
                                );
                            }
                        }
                        break;
                    default:
                        this.drawSliceNormal(
                            canvas,
                            info,
                            destRect,
                        );
                        break;
                }
                break;
        }
        if (evtInfo.isDurationEvent) {
            evt.duration = 2;
        }
        if (evt.duration && evt.duration > 0 && style.Active) {
            const duration = evt[evtInfo.durationKey] as number;
            const durWidth = style.Scale * this.iconSize * duration -
                destRect.width;
            const c = this.ColorOf(evt.tab);
            c.alpha = style.Active ? 192 : 91;
            if (durWidth > 0) {
                this.DrawSlice(
                    canvas,
                    this.evbarea,
                    new Rect(
                        dest.x + destRect.width,
                        dest.y,
                        durWidth,
                        destRect.height,
                    ),
                    style.Scale,
                    PatchStyle.Repeat,
                    c,
                );
            }
        }
        return destRect;
    }
    private static ColorOf(tab: Tab): Color {
        switch (tab) {
            case "sounds":
                return new Color(0xffd82433);
            case "rows":
                return new Color(0xff2c4fd9);
            case "actions":
                return new Color(0xffc543b3);
            case "decorations":
                return new Color(0xff00c459);
            case "rooms":
                return new Color(0xffd8b812);
            case "windows":
                return new Color(0xff50b5d7);
            default:
                return new Color(0xff848484);
        }
    }
    public static DrawBack(
        canvas: CanvasRenderingContext2D,
        destRect: Rect,
        color: Color,
        style: IconStyle,
    ) {
        const outline = "event_outline";
        const back = "event_back";
        this.DrawSlice(
            canvas,
            outline,
            destRect,
            style.Scale,
            PatchStyle.Strentch,
            new Color(style.Active ? 0xffffffff : 0xffa8a8a8),
        );
        destRect.inflate(-style.Scale, -style.Scale);
        this.DrawSlice(
            canvas,
            back,
            destRect,
            style.Scale,
            PatchStyle.Strentch,
            color,
        );
    }
    private static WithState(
        color: Color,
        active: boolean,
        enabled: boolean,
    ): Color {
        const c = enabled ? color.clone() : new Color(0xff848484);
        c.alpha = active ? 192 : 91;
        return c;
    }
}


