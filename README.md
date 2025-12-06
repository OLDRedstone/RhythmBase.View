<p align="center">
  <a href="https://www.nuget.org/packages/RhythmBase.View/"><img src="https://img.shields.io/nuget/v/RhythmBase.View?logo=nuget" alt="Nuget Download"></a>
  <img src="https://img.shields.io/nuget/dt/RhythmBase.View" alt="Downloads"/>
</p>

# RhythmBase.View

This project is only used to draw Rhythm Doctor events with [SkiaSharp](https://github.com/mono/SkiaSharp).
You can see examples [here](/RhythmBase.View.Test1/Program.cs).

| Project             | Description                                         | Status           | Link                                                                       | 
|---------------------|-----------------------------------------------------|------------------|----------------------------------------------------------------------------|
| RhythmBase          | Core library for level editing.                     | WIP              | [Go There](https://github.com/OLDRedstone/RhythmBase)                      |
| RhythmBase.View     | Draw all Rhythm Doctor event elements in SkiaSharp. | WIP              | **You are here**                                                           |
| RhythmBase.Addition | Extensions for levels.                              | *Not disclosed*  | -                                                                          |
| RhythmBase.Interact | Interact with Level editor.                         | *Not disclosed*  | -                                                                          |
| RhythmBase.Hospital | Judgement logic for levels.                         | *Not disclosed*  | -                                                                          |
| RhythmBase.Lite     | Lightweight version of RhythmBase.                  | WIP              | [Go there](https://github.com/RDCN-Community-Developers/RhythmToolkitLite) |
| RhythmBase.Control  | Custom controls.                                    | *Not disclosed*  | -                                                                          |

```mermaid
flowchart RL
RBLite[RhythmBase.Lite]
subgraph RD[Rhythm Doctor]
  RDLE[Rhythm Doctor Level Editor]
end
subgraph AD[Adofai]
  ADLE[Adofai Level Editor]
end
RDL([Rhythm Doctor Level])
ADL([Adofai Level])
subgraph RBTitle[RhythmBase]
  RB[RhythmBase]
  RBAdd[RhythmBase.Addition]
  RBInt[RhythmBase.Interact]
  RBHos[RhythmBase.Hospital]
  RBV[RhythmBase.View]
  subgraph RBC[RhythmBase.Control]
    RBCCore[RhythmBase.Control.Core]
    RBCWPF[RhythmBase.Control.WPF]
    RBCWF[RhythmBase.Control.WinForm]
    RBCAva[RhythmBase.Control.Avalonia]
  end
end

RBLite ---> RDL
RBCWPF & RBCWF & RBCAva --> RBCCore --> RBV
RBV & RBHos & RBAdd & RBInt --> RB ---> RDL & ADL
RBInt ---> RDLE --> RDL
RBInt ---> ADLE --> ADL
```