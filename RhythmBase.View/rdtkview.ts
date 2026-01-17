/// <reference lib="dom" />
import {
    assetLoaded,
    assetSize,
    assetUrl,
    Point,
    Rect,
    Size,
    slices,
} from "./assetManager.ts";
import {
    Color,
    EventInfos,
    EventType,
    SayReadyGetSetGoWords,
    Tab,
    WordInfos,
} from "./definition.ts";

const lineHeight = 6;
const charHeight = 8;
const iconSize = 14;

class IconStyle {
    public enabled: boolean = true;
    public active: boolean = false;
    public hover: boolean = false;
    public scale: number = 1;
    public showDuration: boolean = false;
}

class FilterData {
    public color?: Color;
    public hue: number = 0;
    public saturation: number = 1;
    public brightness: number = 1;
    public getFilter(): string {
        let filter = "";
        if (this.color) {
            filter += `url(#${getColorMatrixId(this.color)}) `;
        }
        if (this.hue !== 0) {
            filter += `hue-rotate(${this.hue * 360}deg) `;
        }
        if (this.saturation !== 1) {
            filter += `saturate(${this.saturation}) `;
        }
        if (this.brightness !== 1) {
            filter += `brightness(${this.brightness}) `;
        }
        return filter.trim() || "none";
    }
}

type HTMLLayerElement = HTMLDivElement & {
    onStateChange?: (style: IconStyle) => void;
    filterData: FilterData;
    updateFilter: () => void;
};

type HTMLEventElement = HTMLLayerElement & {
    onStateChange: (style: IconStyle) => void;
    eventStyle: IconStyle;
    eventData: object;
    moveTo: (x: number, y: number) => void;
};

function withState(color: Color, active: boolean, enabled: boolean): Color {
    return (enabled ? color : new Color(0xff848484))
        .withAlpha(active ? 192 : 91);
}

function createLayerFromBackground(
    key: string,
    bound: Point | Rect | null,
    color: Color | null,
    style: IconStyle,
    backgroundRepeat: boolean = false,
): HTMLLayerElement[] {
    const info = slices.get(key);
    if (!info) return [];
    if (info?.isNinePatch) {
        const rectBound = bound as Rect ??
            new Rect(0, 0, info.bounds.width, info.bounds.height);
        const center = info.center;
        const left = rectBound.left;
        const top = rectBound.top;
        const width = rectBound.width;
        const height = rectBound.height;
        const infoLeft = info.bounds.left;
        const infoTop = info.bounds.top;
        const srcXs = [0, center.left, center.right, info.bounds.width];
        const srcYs = [0, center.top, center.bottom, info.bounds.height];
        const layers: HTMLLayerElement[] = [];
        for (let y = 0; y < 3; y++) {
            const srcTop = srcYs[y];
            const srcHeight = srcYs[y + 1] - srcYs[y];
            const dstTop = top +
                (y < 2 ? srcTop : height - (info.bounds.height - srcYs[y]));
            const dstHeight = y === 1
                ? height - center.top - (info.bounds.height - center.bottom)
                : srcHeight;
            const heightScale = dstHeight / srcHeight;
            for (let x = 0; x < 3; x++) {
                const srcLeft = srcXs[x];
                const srcWidth = srcXs[x + 1] - srcXs[x];
                const dstLeft = left +
                    (x < 2 ? srcLeft : width - (info.bounds.width - srcXs[x]));
                const dstWidth = x === 1
                    ? width - center.left - (info.bounds.width - center.right)
                    : srcWidth;
                const widthScale = dstWidth / srcWidth;
                if (backgroundRepeat && x === 1 && y === 1) {
                    for (let dstt = 0; dstt < dstHeight; dstt += srcHeight) {
                        const dsth = Math.min(srcHeight, dstHeight - dstt);
                        for (let dstl = 0; dstl < dstWidth; dstl += srcWidth) {
                            const dstw = Math.min(srcWidth, dstWidth - dstl);
                            const patchLayer = document.createElement(
                                "div",
                            ) as HTMLLayerElement;
                            patchLayer.filterData = new FilterData();
                            patchLayer.updateFilter = () => {
                                patchLayer.style.filter = patchLayer.filterData
                                    .getFilter();
                            };
                            patchLayer.style.position = "absolute";
                            patchLayer.style.boxSizing = "content-box";
                            patchLayer.style.backgroundClip = "content-box";
                            patchLayer.style.backgroundImage =
                                `url('${assetUrl}')`;
                            patchLayer.style.left = `${
                                (dstLeft + dstl) * style.scale
                            }px`;
                            patchLayer.style.top = `${
                                (dstTop + dstt) * style.scale
                            }px`;
                            patchLayer.style.width = `${dstw * style.scale}px`;
                            patchLayer.style.height = `${dsth * style.scale}px`;
                            patchLayer.style.backgroundPosition = `-${
                                (infoLeft + srcLeft) * style.scale
                            }px -${(infoTop + srcTop) * style.scale}px`;
                            patchLayer.style.backgroundSize = `${
                                assetSize.width * style.scale
                            }px ${assetSize.height * style.scale}px`;
                            layers.push(patchLayer);
                        }
                    }
                } else {
                    const patchLayer = document.createElement(
                        "div",
                    ) as HTMLLayerElement;
                    patchLayer.filterData = new FilterData();
                    patchLayer.updateFilter = () => {
                        patchLayer.style.filter = patchLayer.filterData
                            .getFilter();
                    };
                    patchLayer.style.position = "absolute";
                    patchLayer.style.boxSizing = "content-box";
                    patchLayer.style.backgroundClip = "content-box";
                    patchLayer.style.backgroundImage = `url('${assetUrl}')`;
                    patchLayer.style.left = `${dstLeft * style.scale}px`;
                    patchLayer.style.top = `${dstTop * style.scale}px`;
                    patchLayer.style.width = `${dstWidth * style.scale}px`;
                    patchLayer.style.height = `${dstHeight * style.scale}px`;
                    patchLayer.style.backgroundPosition = `-${
                        (infoLeft + srcLeft) * widthScale * style.scale
                    }px -${(infoTop + srcTop) * heightScale * style.scale}px`;
                    patchLayer.style.backgroundSize = `${
                        assetSize.width * widthScale * style.scale
                    }px ${assetSize.height * heightScale * style.scale}px`;
                    layers.push(patchLayer);
                }
            }
        }

        if (color && Color.White.equals(color) === false) {
            layers.forEach((layer) => {
                (layer as any).onColorChange = (color: Color) => {
                    if (!color) {
                        return;
                    }
                    layer.filterData.color = color;
                    layer.updateFilter();
                };
                (layer as any).onColorChange(color);
            });
        } else {
        }
        return layers;
    } else {
        const pivotX = info.pivot.x ?? 0;
        const pivotY = info.pivot.y ?? 0;
        const layer = document.createElement("div") as HTMLLayerElement;
        layer.filterData = new FilterData();
        layer.updateFilter = () => {
            layer.style.filter = layer.filterData.getFilter();
        };
        layer.style.position = "absolute";
        layer.style.boxSizing = "content-box";
        layer.style.backgroundClip = "content-box";
        layer.style.backgroundImage = `url('${assetUrl}')`;
        if (bound instanceof Rect) {
            const left = bound.left;
            const top = bound.top;
            const width = bound.width;
            const height = bound.height;
            const infoLeft = info.bounds.left;
            const infoTop = info.bounds.top;
            const widthScale = width / info.bounds.width;
            const heightScale = height / info.bounds.height;
            layer.style.left = `${(left - pivotX) * style.scale}px`;
            layer.style.top = `${(top - pivotY) * style.scale}px`;
            layer.style.width = `${width * style.scale}px`;
            layer.style.height = `${height * style.scale}px`;
            layer.style.backgroundPosition = `-${
                infoLeft * widthScale * style.scale
            }px -${infoTop * heightScale * style.scale}px`;
            layer.style.backgroundSize = `${
                assetSize.width * widthScale * style.scale
            }px ${assetSize.height * heightScale * style.scale}px`;
        } else {
            bound = bound as Point ?? new Point(0, 0);
            const left = bound.x;
            const top = bound.y;
            const width = info.bounds.width;
            const height = info.bounds.height;
            const infoLeft = info.bounds.left;
            const infoTop = info.bounds.top;
            layer.style.left = `${(left - pivotX) * style.scale}px`;
            layer.style.top = `${(top - pivotY) * style.scale}px`;
            layer.style.width = `${width * style.scale}px`;
            layer.style.height = `${height * style.scale}px`;
            layer.style.backgroundPosition = `-${infoLeft * style.scale}px -${
                infoTop * style.scale
            }px`;
            layer.style.backgroundSize = `${assetSize.width * style.scale}px ${
                assetSize.height * style.scale
            }px`;
        }
        if (color && Color.White.equals(color) === false) {
            (layer as any).onColorChange = (color: Color) => {
                if (!color) {
                    return;
                }
                layer.filterData.color = color;
                layer.updateFilter();
            };
            (layer as any).onColorChange(color);
        } else {
        }
        return [layer];
    }
}

const colorMatrixIds = new Map<number, string>();
const colorMatrixCache = document.createElement("div");
document.body.appendChild(colorMatrixCache);

function getColorMatrixId(rgba: Color): string {
    if (colorMatrixIds.has(rgba.toUint32())) {
        return colorMatrixIds.get(rgba.toUint32())!;
    }

    const feColorMatrix = document.createElementNS(
        "http://www.w3.org/2000/svg",
        "feColorMatrix",
    );
    feColorMatrix.setAttribute("type", "matrix");
    const r = rgba.r / 255;
    const g = rgba.g / 255;
    const b = rgba.b / 255;
    const a = rgba.a / 255;
    feColorMatrix.setAttribute(
        "values",
        [
            r,
            0,
            0,
            0,
            0,
            0,
            g,
            0,
            0,
            0,
            0,
            0,
            b,
            0,
            0,
            0,
            0,
            0,
            a,
            0,
        ].join(" "),
    );
    feColorMatrix.setAttribute("color-interpolation-filters", "sRGB");
    const filter = document.createElementNS(
        "http://www.w3.org/2000/svg",
        "filter",
    );
    filter.setAttribute("id", `filter_${rgba.toHexString()}`);
    filter.appendChild(feColorMatrix);
    const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
    svg.setAttribute("width", "0");
    svg.setAttribute("height", "0");
    svg.appendChild(filter);
    colorMatrixCache.appendChild(svg);
    colorMatrixIds.set(rgba.toUint32(), filter.getAttribute("id")!);
    return filter.getAttribute("id")!;
}

function measureRDFontText(text: string): number {
    let len = 0;
    for (let i = 0; i < text.length; i++) {
        const char = text.charAt(i);
        if (char === "\n") {
            len = 0;
        } else {
            const charCode = char.charCodeAt(0);
            const charKey = `char_${charCode.toString(16).padStart(4, "0")}`;
            const charInfo = slices.get(charKey);
            len += charInfo?.bounds.width ?? 0;
        }
    }
    return len;
}

function createRDFontLayer(
    text: string,
    style: IconStyle,
    color: Color | null,
): HTMLLayerElement {
    const lineHead = new Point(0, -charHeight);
    const start = lineHead.clone();
    const layer = document.createElement("div") as HTMLLayerElement;
    const charLayers: HTMLLayerElement[] = [];
    layer.style.position = "absolute";
    for (let i = 0; i < text.length; i++) {
        const char = text.charAt(i);
        if (char === "\n") {
            start.x = lineHead.x;
            start.y += lineHeight * style.scale;
        } else {
            const charCode = char.charCodeAt(0);
            const charKey = `char_${charCode.toString(16).padStart(4, "0")}`;
            const charInfo = slices.get(charKey);
            if (charInfo) {
                const charLayer = createLayerFromBackground(
                    charKey,
                    new Point(start.x, start.y),
                    color,
                    style,
                );
                start.x += (charInfo.bounds.width) * style.scale;

                charLayers.push(...charLayer);
            }
        }
    }
    layer.updateFilter = () => {
        charLayers.forEach((cl) => {
            cl.updateFilter();
        });
    };
    charLayers.forEach((cl) => {
        layer.appendChild(cl);
    });
    return layer;
}

function createBackLayer(
    size: Size | null,
    color: Color,
    style: IconStyle,
): HTMLLayerElement[] {
    const outline = "event_outline";
    const back = "event_back";
    const backLayer = createLayerFromBackground(
        back,
        new Rect(
            1,
            1,
            (size?.width ?? iconSize) - 2,
            (size?.height ?? iconSize) - 2,
        ),
        withState(
            color,
            style.active,
            style.enabled,
        ),
        style,
    );
    backLayer.forEach((layer) => {
        if ((layer as any).onColorChange) {
            layer.onStateChange = (
                style: IconStyle,
            ) => (layer as any).onColorChange(withState(
                color,
                style.active,
                style.enabled,
            ));
        }
    });

    const outlineLayer = createLayerFromBackground(
        outline,
        size ? new Rect(size) : null,
        new Color(style.active ? 0xffffffff : 0xffa8a8a8),
        style,
    );
    outlineLayer.forEach((layer) => {
        if ((layer as any).onColorChange) {
            (layer as HTMLLayerElement).onStateChange = (
                style: IconStyle,
            ) => (layer as any).onColorChange(
                new Color(style.active ? 0xffffffff : 0xffa8a8a8),
            );
        }
    });
    return [...backLayer, ...outlineLayer];
}

function colorOf(tab: Tab): Color {
    switch (tab) {
        case Tab.Sounds:
            return new Color("d92433");
        case Tab.Rows:
            return new Color("2c4fdb");
        case Tab.Actions:
            return new Color("c544b3");
        case Tab.Rooms:
            return new Color("d8b811");
        case Tab.Decorations:
            return new Color("00c459");
        case Tab.Windows:
            return new Color("50b5d7");
        default:
            return new Color("7d7d7d");
    }
}

function layerHoverInit(layer: HTMLLayerElement): void {
    layer.style.opacity = "0";
    layer.onStateChange = (style: IconStyle) => {
        if (style.hover) {
            layer.style.opacity = "1.0";
        } else {
            layer.style.opacity = "0";
        }
    };
}

function createHintLayer(
    hint: string,
    width: number,
    color: Color,
): HTMLLayerElement {
    const hintLayer = document.createElement("div") as HTMLLayerElement;
    hintLayer.style.position = "absolute";
    hintLayer.style.left = `${width}px`;
    hintLayer.style.width = `max-content`;
    hintLayer.innerHTML = hint;
    hintLayer.style.fontSize = `${18}px`;
    hintLayer.style.fontFamily = "d9, Arial, sans-serif";
    hintLayer.style.color = color.toRgbaString();
    hintLayer.style.fontWeight = "bold";
    hintLayer.style.textShadow = `
    2px 2px 0 black,
    2px 0px 0 black,
    2px -2px 0 black,
    0px -2px 0 black,
    -2px -2px 0 black,
    -2px 0px 0 black,
    -2px 2px 0 black,
    0px 2px 0 black
    `;
    hintLayer.style.pointerEvents = "none";
    hintLayer.style.opacity = "0";
    hintLayer.style.zIndex = "10";
    hintLayer.onStateChange = (style: IconStyle) => {
        if (style.hover) {
            hintLayer.style.opacity = "1.0";
        } else {
            hintLayer.style.opacity = "0";
        }
    };
    return hintLayer;
}

function createElementEvent(
    obj: any,
    style: IconStyle,
): HTMLEventElement | null {
    style = { ...new IconStyle(), ...style };
    let key0 = `event_${obj.type}`;
    const hasType = EventType.hasOwnProperty(obj.type as string);
    if (!hasType) {
        console.warn(
            obj.type ? `Unknown event type: ${obj.type}` : `Event type missing`,
        );
    }
    if (!slices.has(key0)) {
        if (!slices.has("event_Unknown")) {
            return null;
        }
        key0 = "event_Unknown";
    }
    const evttype = obj.type as EventType ?? EventType.Unknown;

    const slcinfo = slices.get(key0) ?? slices.get("event_Unknown");
    if (!slcinfo) {
        console.warn(`Slice info for event type ${evttype} not found.`);
        return null;
    }
    const evinfo = EventInfos[evttype] ?? EventInfos[EventType.Unknown];

    const key = "event_" + evttype;

    const pulse = "event_beat_pulse";
    const hit = "event_beat_hit";
    const hitf = "event_beat_hit_freeze";
    const hitb = "event_beat_hit_burn";
    const beatcross = "event_beat_cross";
    const beatx = "event_beat_x";
    const beatsynco = "event_beat_synco";
    const beatline = "event_beat_line";
    const evtag = "event_tag";
    const evbarea = "event_beat_area";
    const evbskip = "event_beat_skip";

    const beatColor = new Color(0xff60e345);
    let iconWidth = iconSize;

    const element = document.createElement("div") as HTMLEventElement;
    element.eventStyle = { ...style };

    const layersToAdd: HTMLLayerElement[] = [];

    element.style.imageRendering = "pixelated";
    switch (evttype) {
        case EventType.AddClassicBeat:
            {
                const tick = obj.tick as number ?? 1;
                const swing = (obj.swing as number ?? 0) || 1;
                const hold = obj.hold as number ?? 0;
                const prexs =
                    obj.preXs as { syncoBeat: number; syncoSwing: number } ??
                        { syncoBeat: -1, syncoSwing: 0 };
                const length = obj.length as number ?? 7;
                const iconWidth = iconSize *
                    (tick * (length - 1 - prexs.syncoSwing));
                const holdBar = hold > 0
                    ? createLayerFromBackground(
                        evbarea,
                        new Rect(iconWidth, 0, iconSize * hold, iconSize),
                        new Color(style.active ? 0xffd046f3 : 0xff7e3990),
                        style,
                        true,
                    )
                    : [];
                holdBar.forEach((layer) => {
                    layer.style.pointerEvents = "none";
                    return layer;
                });
                const backLayers = createBackLayer(
                    new Size(iconWidth, iconSize),
                    colorOf(obj.tab as Tab ?? evinfo.defaultTab),
                    style,
                );
                const pulses: HTMLLayerElement[] = [];
                for (let i = 0; i < length - 1; i++) {
                    const pulseLayer = createLayerFromBackground(
                        pulse,
                        new Point(
                            iconSize * (tick *
                                (i + i % 2 * (1 - swing) -
                                    (i <= (prexs.syncoBeat)
                                        ? 0
                                        : (prexs.syncoSwing)))),
                            0,
                        ),
                        null,
                        style,
                    );
                    pulseLayer.forEach((layer) => {
                        layerHoverInit(layer);
                        return layer;
                    });
                    pulses.push(...pulseLayer);
                }
                const hitLayer = createLayerFromBackground(
                    hit,
                    new Point(
                        iconSize *
                                (tick *
                                    (length - 1 - (prexs.syncoSwing ?? 0))) - 1,
                        0,
                    ),
                    null,
                    style,
                );
                layersToAdd.push(...holdBar);
                layersToAdd.push(...backLayers);
                layersToAdd.push(...pulses);
                layersToAdd.push(...hitLayer);
            }
            break;
        case EventType.AddOneshotBeat:
            {
                const tick = obj.tick as number ?? 1;
                const interval = obj.interval as number ?? 2;
                const loop = obj.loops as number ?? 0;
                const subdiv = obj.subdivisions as number ?? 1;
                const hold = obj.hold as boolean ?? false;
                const skip = obj.skipshot as boolean ?? false;
                const mode =
                    obj.freezeBurnMode as "Freezeshot" | "Burnshot" | null ??
                        null;
                const delay = obj.freezeBurnMode === "Freezeshot"
                    ? obj.delay as number ?? 0
                    : 0;
                const subdivWidth = iconSize * (subdiv - 1) / subdiv * tick;
                const eventWidth = iconSize * (interval * loop + tick + delay);
                const off = interval - tick;
                const backLayers = createBackLayer(
                    new Size(
                        eventWidth,
                        iconSize,
                    ),
                    colorOf(obj.tab as Tab ?? evinfo.defaultTab),
                    style,
                );
                const holdWidth = hold
                    ? iconSize * (interval - tick - delay)
                    : 0;
                console.log(holdWidth, subdivWidth);
                const holdLayer = hold && (holdWidth - subdivWidth) > 0
                    ? createLayerFromBackground(
                        evbarea,
                        new Rect(
                            eventWidth + subdivWidth,
                            0,
                            holdWidth - subdivWidth,
                            iconSize,
                        ),
                        new Color(style.enabled ? 0xffd046f3 : 0xff6f6f6f),
                        style,
                        true,
                    )
                    : [];
                holdLayer.forEach((layer) => {
                    layer.style.pointerEvents = "none";
                    return layer;
                });
                const subdivLayer = subdiv > 1
                    ? createLayerFromBackground(
                        evbarea,
                        new Rect(eventWidth, 0, subdivWidth, iconSize),
                        new Color(style.enabled ? 0xff13B021 : 0xff6f6f6f),
                        style,
                        true,
                    )
                    : [];
                subdivLayer.forEach((layer) => {
                    layer.style.pointerEvents = "none";
                    return layer;
                });
                const skipshotLayer = skip
                    ? createLayerFromBackground(
                        evbarea,
                        new Rect(
                            eventWidth + Math.max(subdivWidth, holdWidth),
                            0,
                            iconSize * (interval - delay) -
                                Math.max(subdivWidth, holdWidth),
                            iconSize,
                        ),
                        new Color(style.enabled ? 0xffc53b3b : 0xff6f6f6f),
                        style,
                        true,
                    )
                    : [];
                skipshotLayer.forEach((layer) => {
                    layer.style.pointerEvents = "none";
                    return layer;
                });
                const skipHitLayer = skip
                    ? createLayerFromBackground(
                        evbskip,
                        new Point(
                            eventWidth + iconSize * (interval - delay) - 1,
                            0,
                        ),
                        null,
                        style,
                    )
                    : [];
                skipHitLayer.forEach((layer) => {
                    layer.style.pointerEvents = "none";
                    return layer;
                });
                const pulses: HTMLLayerElement[] = [];
                const crossLayers: HTMLLayerElement[] = [];
                const posx = -off * iconSize;
                const hittype = mode === "Freezeshot"
                    ? hitf
                    : mode === "Burnshot"
                    ? hitb
                    : null;
                for (let l = 0; l <= loop; l++) {
                    for (let i = 0; i < subdiv; i++) {
                        const pulseLayer = createLayerFromBackground(
                            pulse,
                            new Point(
                                iconSize * (l * interval + i * tick / subdiv),
                                0,
                            ),
                            null,
                            style,
                        );
                        pulseLayer.forEach((layer) => {
                            layerHoverInit(layer);
                            return layer;
                        });
                        pulses.push(...pulseLayer);
                        const hitLayer = createLayerFromBackground(
                            hit,
                            new Point(
                                iconSize *
                                        (l * interval + delay + tick +
                                            (i * tick / subdiv)) - 1,
                                0,
                            ),
                            null,
                            style,
                        );
                        pulses.push(...hitLayer);
                    }
                    const crossLayer1 = mode || hold
                        ? createLayerFromBackground(
                            beatcross,
                            new Point(iconSize * (l * interval - off) - 1, 0),
                            null,
                            style,
                        )
                        : [];
                    crossLayer1.forEach((layer) => {
                        layerHoverInit(layer);
                        return layer;
                    });
                    const crossLayer2 = mode
                        ? createLayerFromBackground(
                            beatcross,
                            new Point(
                                iconSize *
                                        (l * interval - off +
                                            (mode === "Freezeshot"
                                                ? delay
                                                : -tick)) +
                                    (mode === "Freezeshot" ? -1 : 0),
                                0,
                            ),
                            null,
                            style,
                        )
                        : [];
                    crossLayer2.forEach((layer) => {
                        layerHoverInit(layer);
                        return layer;
                    });
                    const hitLayer = hittype
                        ? createLayerFromBackground(
                            hittype,
                            new Point(
                                iconSize * (l * interval + tick) - 1,
                                0,
                            ),
                            null,
                            style,
                        )
                        : [];
                    crossLayers.push(...crossLayer1);
                    crossLayers.push(...crossLayer2);
                    pulses.push(...hitLayer);
                }
                const loopLayer = createLayerFromBackground(
                    "event_beat_loop",
                    new Point(
                        eventWidth,
                        0,
                    ),
                    null,
                    style,
                );
                loopLayer.forEach((loopLayer) => {
                    loopLayer.style.opacity = "0";
                    loopLayer.style.zIndex = "5";
                    loopLayer.onStateChange = (
                        style: IconStyle,
                    ) => {
                        if (style.hover) {
                            loopLayer.style.opacity = "1.0";
                        } else {
                            loopLayer.style.opacity = "0";
                        }
                    };
                    return loopLayer;
                });
                layersToAdd.push(...backLayers);
                layersToAdd.push(...holdLayer);
                layersToAdd.push(...subdivLayer);
                layersToAdd.push(...skipshotLayer);
                layersToAdd.push(...skipHitLayer);
                layersToAdd.push(...pulses);
                layersToAdd.push(...loopLayer);
                layersToAdd.push(...crossLayers);
            }
            break;
        case EventType.AddFreeTimeBeat:
            {
                const hold = obj.hold as number ?? 0;
                const holdLayer = hold > 0
                    ? createLayerFromBackground(
                        evbarea,
                        new Rect(
                            slcinfo.bounds.width,
                            0,
                            iconSize * hold -
                                slcinfo.bounds.width,
                            iconSize,
                        ),
                        new Color(style.active ? 0xffd046f3 : 0xff7e3990),
                        style,
                        true,
                    )
                    : [];
                holdLayer.forEach((layer) => {
                    layer.style.pointerEvents = "none";
                    return layer;
                });
                const backLayers = createBackLayer(
                    new Size(slcinfo.bounds.width, iconSize),
                    beatColor,
                    style,
                );
                const hitLayer = obj.pulse === 6
                    ? createLayerFromBackground(
                        hit,
                        new Point(0, 0),
                        null,
                        style,
                    )
                    : [];
                const textLayer = createRDFontLayer(
                    ((obj.pulse as number ?? 0) + 1).toString(),
                    style,
                    null,
                );
                textLayer.style.left = `${1.5 * style.scale}px`;
                textLayer.style.top = `${10 * style.scale}px`;
                layersToAdd.push(...holdLayer);
                layersToAdd.push(...backLayers);
                layersToAdd.push(...hitLayer);
                layersToAdd.push(textLayer);
            }
            break;
        case EventType.Comment:
            {
                const backLayers = createBackLayer(
                    slcinfo.bounds.size,
                    new Color(obj.color as string ?? "7d7d7d"),
                    style,
                );
                const iconLayer = createLayerFromBackground(
                    key,
                    null,
                    null,
                    style,
                );
                layersToAdd.push(...backLayers);
                layersToAdd.push(...iconLayer);
            }
            break;
        case EventType.DesktopColor:
            {
                const backLayers = createBackLayer(
                    slcinfo.bounds.size,
                    colorOf(obj.tab as Tab ?? evinfo.defaultTab),
                    style,
                );
                const iconColorLayer = createLayerFromBackground(
                    `${key}_0`,
                    new Point(0, iconSize),
                    new Color(obj.endColor as string ?? "ffffff"),
                    style,
                );
                const iconLayer = createLayerFromBackground(
                    `${key}_1`,
                    new Point(0, iconSize),
                    null,
                    style,
                );
                layersToAdd.push(...backLayers);
                layersToAdd.push(...iconColorLayer);
                layersToAdd.push(...iconLayer);
            }
            break;
        case EventType.PulseFreeTimeBeat:
            {
                const hold = obj.hold as number ?? 0;
                const holdLayer = hold > 0
                    ? createLayerFromBackground(
                        evbarea,
                        new Rect(
                            slcinfo.bounds.width,
                            0,
                            iconSize * hold -
                                slcinfo.bounds.width,
                            slcinfo.bounds.height,
                        ),
                        new Color(style.active ? 0xffd046f3 : 0xff7e3990),
                        style,
                        true,
                    )
                    : [];
                holdLayer.forEach((layer) => {
                    layer.style.pointerEvents = "none";
                    return layer;
                });
                const backLayers = createBackLayer(
                    new Size(slcinfo.bounds.width, slcinfo.bounds.height),
                    beatColor,
                    style,
                );
                const hitLayer = obj.pulse === 6
                    ? createLayerFromBackground(
                        hit,
                        new Point(0, 0),
                        null,
                        style,
                    )
                    : [];
                let text = "";
                switch (obj.action as string) {
                    case "Custom":
                        text = ((obj.customPulse as number ?? 0) + 1)
                            .toString();
                        break;
                    case "Increment":
                        text = ">";
                        break;
                    case "Decrement":
                        text = "<";
                        break;
                    case "Remove":
                        text = "x";
                        break;
                }

                const textLayer = createRDFontLayer(
                    text,
                    style,
                    null,
                );
                textLayer.style.left = `${1.5 * style.scale}px`;
                textLayer.style.top = `${10 * style.scale}px`;
                layersToAdd.push(...holdLayer);
                layersToAdd.push(...backLayers);
                layersToAdd.push(...hitLayer);
                layersToAdd.push(textLayer);
            }
            break;
        case EventType.ReorderRooms:
        case EventType.ReorderWindows:
            {
                const backLayers = createBackLayer(
                    slcinfo.bounds.size,
                    colorOf(obj.tab as Tab ?? evinfo.defaultTab),
                    style,
                );
                const orderLayers: HTMLLayerElement[] = [];
                const order = obj.order as number[] ?? [0, 1, 2, 3];
                for (let i = 0; i < order.length; i++) {
                    {
                        const orderLayer = createLayerFromBackground(
                            `${key}_${order[i]}`,
                            new Point(0, i * iconSize),
                            null,
                            style,
                        );
                        orderLayers.push(...orderLayer);
                    }
                }
                layersToAdd.push(...backLayers);
                layersToAdd.push(...orderLayers);
            }
            break;
        case EventType.SayReadyGetSetGo:
            {
                const wordInfo = WordInfos[
                    obj.phraseToSay as SayReadyGetSetGoWords ??
                        SayReadyGetSetGoWords.JustSayGo
                ];
                const len = wordInfo.length * (obj.tick as number ?? 0) + 1;
                iconWidth = len * iconSize;
                const stringToJoin = wordInfo.phrase.split(" ");
                const stringToDraw = [stringToJoin[0]];
                let lw = measureRDFontText(stringToJoin[0]);
                const sw = measureRDFontText(" ");
                const backLayers = createBackLayer(
                    new Size(len * iconSize, iconSize),
                    colorOf(obj.tab as Tab ?? evinfo.defaultTab),
                    style,
                );
                const wordLayers: HTMLLayerElement[] = [];
                for (let i = 1; i < stringToJoin.length; i++) {
                    const part = stringToJoin[i];
                    const w = measureRDFontText(part);
                    if (lw + sw + w > (len * iconSize) * style.scale) {
                        lw = w;
                        stringToDraw.push(part);
                    } else {
                        lw += sw + w;
                        stringToDraw[stringToDraw.length - 1] += " " + part;
                    }
                }
                const c = Math.min(stringToDraw.length, 3);
                let top = iconSize / 2 - charHeight * c / 4 + 1;
                for (let i = 0; i < c; i++) {
                    const line = stringToDraw[i];
                    const p = new Point(
                        (len * iconSize - measureRDFontText(line) / 2) / 2,
                        top + (i * charHeight + lineHeight) / 2,
                    );
                    const wordLayer = createRDFontLayer(
                        line,
                        { ...style, scale: style.scale / 2 },
                        null,
                    );
                    wordLayer.style.left = `${p.x * style.scale}px`;
                    wordLayer.style.top = `${p.y * style.scale}px`;
                    wordLayers.push(wordLayer);
                }
                layersToAdd.push(...backLayers);
                layersToAdd.push(...wordLayers);
            }
            break;
        case EventType.SetRowXs:
            {
                const syncoBeat = obj.syncoBeat as number ?? -1;
                const width = slcinfo.bounds.width / 6;
                const iconBounds = slices.get(beatx)!.bounds;
                const s = width / iconBounds.width;
                let left = 0;
                const top = iconSize / 2 - iconBounds.height * s / 2;
                const patternLayers: HTMLLayerElement[] = [];
                const backLayers = createBackLayer(
                    new Size(slcinfo.bounds.width, slcinfo.bounds.height),
                    colorOf(obj.tab as Tab ?? evinfo.defaultTab),
                    style,
                );
                if (
                    typeof obj.pattern !== "string" || obj.pattern.length !== 6
                ) {
                    obj.pattern = "------";
                }
                for (let p of obj.pattern as string) {
                    const beatxLayer = createLayerFromBackground(
                        p === "x" ? beatx : beatline,
                        new Rect(left, top, width, iconBounds.height * s),
                        null,
                        style,
                    );
                    patternLayers.push(...beatxLayer);
                    left += width;
                }
                const syncoBeatLayer = syncoBeat >= 0
                    ? createLayerFromBackground(
                        beatsynco,
                        new Rect(
                            width * syncoBeat,
                            top,
                            width,
                            iconBounds.height * s,
                        ),
                        null,
                        style,
                    )
                    : [];
                layersToAdd.push(...backLayers);
                layersToAdd.push(...patternLayers);
                layersToAdd.push(...syncoBeatLayer);
            }
            break;
        case EventType.ShowRooms:
            {
                const backLayers = createBackLayer(
                    new Size(iconSize, iconSize * 4),
                    colorOf(obj.tab as Tab ?? evinfo.defaultTab),
                    style,
                );
                const roomLayers: HTMLLayerElement[] = [];
                for (let i = 0; i < 4; i++) {
                    const roomLayer = createLayerFromBackground(
                        `${key}_${
                            (obj.rooms as number[] ?? []).indexOf(i) >= 0
                                ? "1"
                                : "0"
                        }`,
                        new Point(0, i * iconSize * style.scale),
                        null,
                        { ...style, scale: style.scale / 2 },
                    );
                    roomLayers.push(...roomLayer);
                }
                layersToAdd.push(...backLayers);
                layersToAdd.push(...roomLayers);
            }
            break;
        default:
            const backLayers = createBackLayer(
                slcinfo.bounds.size,
                colorOf(obj.tab as Tab ?? evinfo.defaultTab),
                style,
            );
            layersToAdd.push(...backLayers);

            switch (evttype) {
                case EventType.CustomFlash:
                    const iconLayer = createLayerFromBackground(
                        key,
                        null,
                        null,
                        style,
                    );
                    const startColorLayer = createLayerFromBackground(
                        `${key}_0`,
                        null,
                        new Color(obj.startColor as string ?? "000000")
                            .withAlpha(
                                ((obj.startOpacity as number) ?? 0) * 255,
                            ),
                        style,
                    );
                    const endColorLayer = createLayerFromBackground(
                        `${key}_1`,
                        null,
                        new Color(obj.endColor as string ?? "000000")
                            .withAlpha(
                                ((obj.endOpacity as number) ?? 0) * 255,
                            ),
                        style,
                    );
                    layersToAdd.push(...iconLayer);
                    layersToAdd.push(...startColorLayer);
                    layersToAdd.push(...endColorLayer);
                    break;
                case EventType.FlipScreen:
                    {
                        const flip = ((obj.flipX as boolean) ? 1 : 0) |
                            ((obj.flipY as boolean) ? 2 : 0);
                        const iconLayer = createLayerFromBackground(
                            flip === 0 ? `${key}_0` : `${key}_${flip - 1}`,
                            null,
                            null,
                            style,
                        );
                        layersToAdd.push(...iconLayer);
                    }
                    break;
                case EventType.FloatingText:
                    {
                        const backColorLayer = createLayerFromBackground(
                            `${key}_0`,
                            null,
                            Color.FromRgba(obj.color as string ?? "FFFFFFFF"),
                            style,
                        );
                        const foreColorLayer = createLayerFromBackground(
                            `${key}_1`,
                            null,
                            Color.FromRgba(
                                obj.outlineColor as string ?? "FFFFFFFF",
                            ),
                            style,
                        );
                        layersToAdd.push(...backColorLayer);
                        layersToAdd.push(...foreColorLayer);
                    }
                    break;
                case EventType.MoveRoom:
                    {
                        obj.scale ??= [null, null];
                        const hasPivot = obj.pivot !== undefined;
                        obj.pivot ??= [null, null];
                        const degree = obj.angle as number ?? 0;
                        const radius = -degree * Math.PI / 180;
                        let width =
                            (obj.scale
                                ? ((obj.scale as number[])[0] ?? 0)
                                : 100) / 100;
                        let height =
                            (obj.scale
                                ? ((obj.scale as number[])[1] ?? 0)
                                : 100) / 100;
                        const pleft =
                            (obj.pivot
                                ? ((obj.pivot as number[])[0] ?? 0)
                                : 50) / 100;
                        const ptop =
                            (obj.pivot
                                ? ((obj.pivot as number[])[1] ?? 0)
                                : 50) / 100;
                        if (width === 0 || height === 0) break;
                        const uniform = Math.sqrt(
                            width * width + height * height,
                        );
                        width /= uniform;
                        height /= uniform;
                        const roomLayer = createLayerFromBackground(
                            key,
                            new Rect(
                                iconSize / 2,
                                iconSize / 2,
                                iconSize * width,
                                iconSize * height,
                            ),
                            null,
                            style,
                        );
                        roomLayer.forEach((roomLayer) => {
                            roomLayer.style.transform =
                                `translate(-50%, -50%) ` +
                                `rotate(${-degree}deg)`;
                            return roomLayer;
                        });
                        layersToAdd.push(...roomLayer);
                        if (hasPivot) {
                            let pivotx = width * (1 - pleft * 2);
                            let pivoty = height * (1 - ptop * 2);
                            const widthrotated = pivotx * Math.cos(radius) +
                                pivoty * Math.sin(radius);
                            const heightrotated = -pivotx * Math.sin(radius) +
                                pivoty * Math.cos(radius);
                            const pointLayer = document.createElement(
                                "div",
                            ) as HTMLLayerElement;
                            pointLayer.style.position = "absolute";
                            pointLayer.style.width = `${style.scale}px`;
                            pointLayer.style.height = `${style.scale}px`;
                            pointLayer.style.left = `${
                                iconSize * (1 - widthrotated) * style.scale / 2
                            }px`;
                            pointLayer.style.top = `${
                                iconSize * (1 + heightrotated) * style.scale / 2
                            }px`;
                            pointLayer.style.backgroundColor = "#FF0000";
                            layersToAdd.push(pointLayer);
                        }
                    }
                    break;
                case EventType.PaintHands:
                case EventType.Tint:
                case EventType.TintRows:
                    {
                        const border = obj.border as "Glow" | "Outline" | null;
                        const backColorLayer = createLayerFromBackground(
                            key,
                            null,
                            Color.FromRgba(
                                obj.tintColor as string ?? "FFFFFFFF",
                            ),
                            style,
                        );
                        const foreColorLayer = border
                            ? createLayerFromBackground(
                                border === "Outline"
                                    ? `${key}_0`
                                    : border === "Glow"
                                    ? `${key}_1`
                                    : "",
                                null,
                                Color.FromRgba(
                                    obj.borderColor as string ?? "FFFFFFFF",
                                ),
                                style,
                            )
                            : [];
                        layersToAdd.push(...backColorLayer);
                        layersToAdd.push(...foreColorLayer);
                    }
                    break;
                case EventType.SetBackgroundColor:
                    {
                        const backType =
                            (obj.backgroundType as "Color" | "Image" | null) ??
                                "Color";
                        const iconLayer = createLayerFromBackground(
                            key,
                            null,
                            null,
                            style,
                        );
                        const images = (obj.image as string[]) ?? [];
                        const imagePath = images.length > 0 ? images[0] : "";
                        const contentInfo = slices.get(`${key}_1`);
                        const colorLayer = createLayerFromBackground(
                            `${key}_0`,
                            null,
                            Color.FromRgba(
                                (backType === "Color"
                                    ? (obj.color as string)
                                    : null) ?? "FFFFFF",
                            ),
                            style,
                        );
                        const contentLayer = backType === "Image"
                            ? createLayerFromBackground(
                                "",
                                contentInfo?.bounds
                                    ? new Rect(contentInfo?.bounds.size)
                                    : null,
                                null,
                                style,
                            )
                            : [];
                        contentLayer.forEach((contentLayer) => {
                            if (backType === "Image" && contentLayer) {
                                contentLayer.style.backgroundImage =
                                    `url(${imagePath})`;
                                contentLayer.style.left = `${
                                    -(contentInfo?.pivot.x ?? 0) *
                                    style.scale
                                }px`;
                                contentLayer.style.top = `${
                                    -(contentInfo?.pivot.y ?? 0) *
                                    style.scale
                                }px`;
                            }
                            return contentLayer;
                        });
                        layersToAdd.push(...iconLayer);
                        layersToAdd.push(...colorLayer);
                        layersToAdd.push(...contentLayer);
                    }
                    break;
                case EventType.SetCrotchetsPerBar:
                    {
                        const cpb = obj.crotchetsPerBar as number ?? 0;
                        const iconLayer = createLayerFromBackground(
                            key,
                            null,
                            null,
                            style,
                        );
                        const cpbLayer = createRDFontLayer(
                            cpb > 9 ? "-" : cpb.toString(),
                            style,
                            Color.Black,
                        );
                        cpbLayer.style.left = `${2 * style.scale}px`;
                        cpbLayer.style.top = `${9 * style.scale}px`;
                        const bLayer = createRDFontLayer(
                            "4",
                            style,
                            Color.Black,
                        );
                        bLayer.style.left = `${8 * style.scale}px`;
                        bLayer.style.top = `${14 * style.scale}px`;
                        layersToAdd.push(...iconLayer);
                        layersToAdd.push(cpbLayer);
                        layersToAdd.push(bLayer);
                    }
                    break;
                case EventType.SetForeground:
                    {
                        const iconLayer = createLayerFromBackground(
                            key,
                            null,
                            null,
                            style,
                        );
                        const images = (obj.image as string[]) ?? [];
                        const imagePath = images.length > 0 ? images[0] : "";
                        const contentInfo = slices.get(`${key}_1`);
                        const colorLayer = imagePath
                            ? createLayerFromBackground(
                                `${key}_0`,
                                null,
                                Color.Black,
                                style,
                            )
                            : [];
                        const contentLayer = imagePath
                            ? createLayerFromBackground(
                                "",
                                contentInfo?.bounds
                                    ? new Rect(contentInfo?.bounds.size)
                                    : null,
                                null,
                                style,
                            )
                            : [];
                        contentLayer.forEach((contentLayer) => {
                            if (contentLayer) {
                                contentLayer.style.backgroundImage =
                                    `url(${imagePath})`;
                                contentLayer.style.left = `${
                                    -(contentInfo?.pivot.x ?? 0) *
                                    style.scale
                                }px`;
                                contentLayer.style.top = `${
                                    -(contentInfo?.pivot.y ?? 0) *
                                    style.scale
                                }px`;
                            }
                            return contentLayer;
                        });
                        layersToAdd.push(...iconLayer);
                        layersToAdd.push(...colorLayer);
                        layersToAdd.push(...contentLayer);
                    }
                    break;
                default:
                    const layer = createLayerFromBackground(
                        hasType ? key : "event_Unknown",
                        null,
                        null,
                        style,
                    );
                    layersToAdd.push(...layer);
                    break;
            }
            break;
    }
    if (evinfo.isDurationEvent) {
        const duration = obj[evinfo.durationKey] as number ?? 0;
        const durWidth = iconSize * duration;
        const durationLayer = durWidth > slcinfo.bounds.width
            ? createLayerFromBackground(
                evbarea,
                new Rect(
                    slcinfo.bounds.width,
                    0,
                    durWidth - slcinfo.bounds.width,
                    slcinfo.bounds.height,
                ),
                colorOf(obj.tab as Tab ?? evinfo.defaultTab),
                style,
                true,
            )
            : [];
        durationLayer.forEach((layer) => {
            layer.style.pointerEvents = "none";
            layerHoverInit(layer);
            return layer;
        });
        layersToAdd.push(...durationLayer);
    }
    const rooms = obj.rooms as number[] ?? [];
    const roomEnabled = new Color(0xffd8b811);
    const roomDisabled = new Color(0xff5b5b5b);
    if (evinfo.isRoomEvent) {
        const roomLayers = [];
        for (let i = 0; i < 4; i++) {
            const roomLayer = createLayerFromBackground(
                `room_${i}`,
                new Point(iconWidth, 0),
                rooms.indexOf(i) > -1 ? roomEnabled : roomDisabled,
                style,
            );
            roomLayers.push(...roomLayer);
        }
        const roomTopLayer = rooms.indexOf(4) > -1
            ? createLayerFromBackground(
                `room_top`,
                new Point(iconWidth, 0),
                roomEnabled,
                style,
            )
            : [];
        roomLayers.push(...roomTopLayer);
        roomLayers.forEach((roomLayer) => {
            if (roomLayer) {
                layerHoverInit(roomLayer);
            }
        });
        layersToAdd.push(...roomLayers);
    }
    const condition = obj.if as string ?? "";
    if (condition.length > 0) {
        const positive = /(?<!~)\d(?!$)/g.test(condition);
        const negative = /~\d(?!$)/g.test(condition);
        const conditionLayer = createLayerFromBackground(
            evtag,
            null,
            positive
                ? negative ? new Color(0xffffff00) : new Color(0xff00ffff)
                : negative
                ? new Color(0xffff0000)
                : null,
            style,
        );
        layersToAdd.push(...conditionLayer);
    }
    const tagLayer = obj.tag
        ? createLayerFromBackground(
            `${evtag}_0`,
            new Point(0, slcinfo.bounds.height),
            new Color(0xffffc786),
            style,
        )
        : [];
    layersToAdd.push(...tagLayer);

    let hintLayer: HTMLLayerElement | null = null;

    switch (evttype) {
        case EventType.Comment:
            hintLayer = createHintLayer(
                obj.text as string ?? "",
                slcinfo.bounds.width * style.scale,
                new Color(obj.color as string ?? "7d7d7d"),
            );
            break;
        case EventType.FloatingText:
        case EventType.AdvanceText:
        case EventType.ShowStatusSign:
        case EventType.TextExplosion:
        case EventType.ShowDialogue:
        case EventType.ReadNarration:
        case EventType.RenameWindow:
            hintLayer = createHintLayer(
                obj.text as string ?? "",
                slcinfo.bounds.width * style.scale,
                colorOf(obj.tab as Tab ?? evinfo.defaultTab),
            );
            break;
        case EventType.PlaySong:
            hintLayer = createHintLayer(
                obj.song?.filename as string ?? "",
                slcinfo.bounds.width * style.scale,
                colorOf(obj.tab as Tab ?? evinfo.defaultTab),
            );
            break;
        case EventType.CallCustomMethod:
            hintLayer = createHintLayer(
                obj.methodName as string ?? "",
                slcinfo.bounds.width * style.scale,
                colorOf(obj.tab as Tab ?? evinfo.defaultTab),
            );
            break;
        case EventType.TagAction:
            hintLayer = createHintLayer(
                obj.Tag as string ?? "",
                slcinfo.bounds.width * style.scale,
                colorOf(obj.tab as Tab ?? evinfo.defaultTab),
            );
            break;
        case EventType.PlayAnimation:
        case EventType.PlayExpression:
            hintLayer = createHintLayer(
                obj.expression as string ?? "",
                slcinfo.bounds.width * style.scale,
                colorOf(obj.tab as Tab ?? evinfo.defaultTab),
            );
            break;
        default:
            if (!hasType) {
                hintLayer = createHintLayer(
                    obj.type && obj.type.toString().length > 0
                        ? `Error: Unknown type: ${obj.type as string}`
                        : "Error: Type missing",
                    slcinfo.bounds.width * style.scale,
                    colorOf(obj.tab as Tab ?? evinfo.defaultTab),
                );
            }
            break;
    }
    if (hintLayer) {
        layersToAdd.push(hintLayer);
    }

    if (obj["$style"]) {
        const hsb = obj["$style"].hsb as number[] ?? [0, 1, 1];
        if (
            hsb.length === 3 && (hsb[0] !== 0 || hsb[1] !== 0 || hsb[2] !== 0)
        ) {
            layersToAdd.forEach((layer) => {
                if (!layer.filterData) {
                    return;
                }
                layer.filterData.hue = hsb[0];
                layer.filterData.saturation = hsb[1];
                layer.filterData.brightness = hsb[2];
            });
        }
        layersToAdd.forEach((layer) => {
            layer.updateFilter?.();
        });
    }

    element.onStateChange = (style: IconStyle) => {
        style = { ...style };
        layersToAdd.forEach((layer) => {
            if (layer.onStateChange) {
                layer.onStateChange(style);
            }
        });
    };
    element.moveTo = function (x: number, y: number) {
        this.style.left = `${x}px`;
        this.style.top = `${y}px`;
    };
    for (const layer of layersToAdd) {
        if (layer) {
            element.appendChild(layer);
        }
    }

    element.eventData = obj;
    return element;
}
export { colorOf, createElementEvent, HTMLEventElement, IconStyle };
