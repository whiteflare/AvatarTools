------------------------------
AvatarCopyUtility
------------------------------

# これは何ですか？

VRChat の各アバター間でコンポーネントをコピーするツールです。
前提として VRCSDK3_Avatar が必要です。

# 何ができますか？

VRCPhysBone や Constraint の一括コピーおよび参照の貼り直しができます。

コピー可能なコンポーネントは以下のとおりです。
- VRC_AvatarDescriptor
- SkinnedMeshRenderer
- MeshRenderer
- VRCPhysBone
- VRCPhysBoneCollider
- VRCContactSender
- VRCContactReceiver
- PositionConstraint
- RotationConstraint
- ScaleConstraint
- ParentConstraint
- LookAtConstraint
- AimConstraint

なお Cloth, DynamicBone のコピーには非対応です。

# 著作権とライセンスは何ですか？

MIT LICENSE での公開です。LICENSE.txt もご確認ください。
Copyright (C) 2022-2023 whiteflare
