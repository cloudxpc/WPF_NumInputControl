# WPF_NumInputControl


This is a WPF custom control which lets you input numberic value by dragging up and down. And, it also provides few small features. See gif example below.

![demo.gif](/Imgs/demo.gif)

**Usage:**

```
<control:DragInput>
  <Label Width="100" Content="{Binding Value, RelativeSource={RelativeSource AncestorType=control:DragInput}}"/>
</control:DragInput>
```

```
<control:DragInput MaxValue="100" MinValue="0" Step="5" Precision="3">
  <TextBox Width="100" Text="{Binding Value, RelativeSource={RelativeSource AncestorType=control:DragInput}}"/>
</control:DragInput>
```
