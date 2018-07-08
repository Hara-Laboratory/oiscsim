using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interface.Assemble
{
    public class VariableAnalyzeInfo
    {
        public abstract class ElementBase
        {
            public int Index;
            public bool NeedInitialization = true;
            public List<Variable> GroupedVariables = new List<Variable>();
            private AddressSymbolInfo _placedInfo = new AddressSymbolInfo();
            public AddressSymbolInfo PlacedInfo
            {
                get
                {
                    return _placedInfo;
                }
                set
                {
                    this._placedInfo = value;

                    for (int i = 0; i < GroupedVariables.Count; i++)
                    {
                        if (GroupedVariables[i].AnalyzeResults[0] != this)
                            continue; //配列変数に関しては一番先頭の要素のみ受け付ける(配列は連続した領域に配置される前提があるため)

                        GroupedVariables[i].PlacedInfo.MemoryNumber = value.MemoryNumber;
                        GroupedVariables[i].PlacedInfo.Address = new Misc.AddressRange()
                        {
                            From = value.Address.From,
                            To = value.Address.From + (uint)GroupedVariables[i].InitialValues.Length - 1
                        };
                    }
                }
            }

            public abstract uint GetInitialValue(int bytesPerWord = 1, int addressTranslationDiv = 1);
        }
        public class ReadonlyElement : ElementBase
        {
            public ValueBase Content;

            public ReadonlyElement()
            {
            }

            public override uint GetInitialValue(int bytesPerWord = 1, int addressTranslationDiv = 1)
            {
                return Content.GetValue(bytesPerWord, addressTranslationDiv);
            }

            public bool IsMatchByContent(ValueBase v)
            {
                if (!this.Content.Equals(v))
                    return false;
                return true;
            }
        }
        public class ReadwriteElement : ElementBase
        {
            public ValueBase InitialValue;

            public override uint GetInitialValue(int bytesPerWord = 1, int addressTranslationDiv = 1)
            {
                return InitialValue.GetValue(bytesPerWord, addressTranslationDiv);
            }
        }
        public List<ReadonlyElement> Readonlys = new List<ReadonlyElement>();
        public List<ReadwriteElement> Readwrites = new List<ReadwriteElement>();
        public List<ReadwriteElement[]> ReadwritePositionSpecifiedHead = new List<ReadwriteElement[]>();
        public List<ReadwriteElement[]> ReadwritePositionSpecifiedTail = new List<ReadwriteElement[]>();


        public VariableAnalyzeInfo()
        {
        }

        /// <summary>
        /// 読み込み専用の変数を登録します
        /// </summary>
        public ElementBase[] RegisterReadonlyVariable(Variable varbl)
        {
            if (varbl.InitialValues.Length == 1)
            { //単一宣言 - 既存のメモリ領域とできるだけ結合する
                ReadonlyElement target;
                if (!FindReadonlyElement(varbl.InitialValues[0],out target))
                {
                    target = new ReadonlyElement();
                    target.Index = Readonlys.Count;
                    Readonlys.Add(target);
                }

                target.Content = varbl.InitialValues[0];
                target.GroupedVariables.Add(varbl);
                varbl.AnalyzeResults = new ElementBase[1] { target };
                return varbl.AnalyzeResults;
            }
            else
            { //配列宣言 - 連続したメモリ領域を確保する
                ElementBase[] res = new ElementBase[varbl.InitialValues.Length];
                for (int contentIdx = 0; contentIdx < varbl.InitialValues.Length; contentIdx++)
                {
                    ReadonlyElement target = new ReadonlyElement();
                    target.Index = Readonlys.Count;
                    Readonlys.Add(target);

                    target.Content = varbl.InitialValues[contentIdx];
                    target.GroupedVariables.Add(varbl);
                    res[contentIdx] = target;
                }

                varbl.AnalyzeResults = res;
                return varbl.AnalyzeResults;
            }
        }
        
        /// <summary>
        /// 読み書き可能な変数を登録し新たな領域を予約します
        /// </summary>
        public ElementBase[] RegisterReadwriteVariableAsNew(Variable varbl)
        {
            if (varbl.InitialValues.Length == 1)
            { //単一宣言
                ReadwriteElement target = new ReadwriteElement();
                target.Index = Readwrites.Count;
                Readwrites.Add(target);

                target.InitialValue = varbl.InitialValues[0];
                target.GroupedVariables.Add(varbl);
                varbl.AnalyzeResults = new ElementBase[1] { target };
                return varbl.AnalyzeResults;
            }
            else
            { //配列宣言
                ElementBase[] res = new ElementBase[varbl.InitialValues.Length];
                for (int contentIdx = 0; contentIdx < varbl.InitialValues.Length; contentIdx++)
                {
                    ReadwriteElement target = new ReadwriteElement();
                    target.Index = Readwrites.Count;
                    Readwrites.Add(target);

                    target.InitialValue = varbl.InitialValues[contentIdx];
                    target.GroupedVariables.Add(varbl);
                    res[contentIdx] = target;
                }

                varbl.AnalyzeResults = res;
                return varbl.AnalyzeResults;
            }
        }

        /// <summary>
        /// 読み書き可能で初期値を持たない変数のための領域を確保します
        /// </summary>
        /// <returns>確保された領域の先頭インデックス</returns>
        public int RegisterReadwriteEmptyRange(int count)
        {
            int res = Readwrites.Count;

            for (int i = 0; i < count; i++)
            {
                ReadwriteElement target = new ReadwriteElement();
                target.Index = Readwrites.Count;
                Readwrites.Add(target);

                target.InitialValue = ValueBase.Zero;
                target.NeedInitialization = false;
            }

            return res;
        }

        /// <summary>
        /// すでに確保済みの領域を指定した変数が利用できるように設定します
        /// </summary>
        public ElementBase[] RegisterReadwriteVariableOverbind(Variable varbl,int startIdx)
        {
            if (varbl.InitialValues.Length == 1)
            { //単一宣言
                var target = Readwrites[startIdx];
                target.GroupedVariables.Add(varbl);
                varbl.AnalyzeResults = new ElementBase[1] { target };
                return varbl.AnalyzeResults;
            }
            else
            { //配列宣言
                ElementBase[] res = new ElementBase[varbl.InitialValues.Length];
                for (int contentIdx = 0; contentIdx < varbl.InitialValues.Length; contentIdx++)
                {
                    var target = Readwrites[startIdx + contentIdx];
                    target.GroupedVariables.Add(varbl);
                    res[contentIdx] = target;
                }

                varbl.AnalyzeResults = res;
                return varbl.AnalyzeResults;
            }
        }

        /// <summary>
        /// 配置位置(PositionHint)が指定された変数のための領域を確保します
        /// </summary>
        public ElementBase[] RegisterReadwriteVariablePositionSpecified(Variable varbl)
        {
            if (varbl.InitialValues.Length == 1)
            { //単一宣言
                ReadwriteElement target = new ReadwriteElement();
                target.Index = 0;
                target.InitialValue = varbl.InitialValues[0];
                target.GroupedVariables.Add(varbl);
                ReadwriteElement[] res = new ReadwriteElement[1] { target };

                varbl.AnalyzeResults = res;
                if (varbl.PositionHint < 0)
                    ReadwritePositionSpecifiedHead.Add(res);
                else
                    ReadwritePositionSpecifiedTail.Add(res);
                return res;
            }
            else
            { //配列宣言
                ReadwriteElement[] res = new ReadwriteElement[varbl.InitialValues.Length];
                for (int contentIdx = 0; contentIdx < varbl.InitialValues.Length; contentIdx++)
                {
                    ReadwriteElement target = new ReadwriteElement();
                    target.Index = contentIdx;

                    target.InitialValue = varbl.InitialValues[contentIdx];
                    target.GroupedVariables.Add(varbl);
                    res[contentIdx] = target;
                }

                varbl.AnalyzeResults = res;
                if (varbl.PositionHint < 0)
                    ReadwritePositionSpecifiedHead.Add(res);
                else
                    ReadwritePositionSpecifiedTail.Add(res);
                return res;
            }
        }

        public bool FindReadonlyElement(ValueBase val,out ReadonlyElement res)
        {
            foreach (var e in Readonlys)
            {
                if (!e.IsMatchByContent(val))
                    continue;

                res = e;
                return true;
            }

            res = null;
            return false;
        }

        public void FlushPositionSpecifiedElements()
        {
            //退避されている位置指定付き変数をReadwritesに配置します

            //まず配置位置でソート
            ReadwritePositionSpecifiedHead.Sort((l,r) => { return r[0].GroupedVariables[0].PositionHint - l[0].GroupedVariables[0].PositionHint; });
            ReadwritePositionSpecifiedTail.Sort((l,r) => { return l[0].GroupedVariables[0].PositionHint - r[0].GroupedVariables[0].PositionHint; });

            //Readwritesに順に配置する その際インデックスを更新していく
            {
                int inserted = 0;
                foreach (var es in ReadwritePositionSpecifiedHead)
                {
                    for (int i = 0; i < es.Length; i++)
                    {
                        ElementBase e = es[i];
                        e.Index = inserted;
                        inserted++;
                    }

                    Readwrites.InsertRange(0,es);
                }

                for (int i = inserted; i < Readwrites.Count; i++)
                {
                    Readwrites[i].Index = i;
                }
            }

            //すでに配置済みの領域のインデックスを更新する
            {
                foreach (var es in ReadwritePositionSpecifiedTail)
                {
                    for (int i = 0; i < es.Length; i++)
                    {
                        ElementBase e = es[i];
                        e.Index = Readwrites.Count + i;
                    }

                    Readwrites.AddRange(es);
                }
            }
        }

        public void ShowAnalyzeResult(enumMessageLevel level)
        {
            {
                MessageManager.ShowLine("",level);
                MessageManager.ShowLine("Constants Mapping:",level);

                {
                    for (int i = 0; i < Readonlys.Count; i++)
                    {
                        var e = Readonlys[i];
                        int tailIdx = i;
                        for (int cursor = i + 1; cursor < Readonlys.Count; cursor++)
                        {
                            var tail = Readonlys[cursor];
                            if (e.GroupedVariables.Count > 1 ||
                                tail.GroupedVariables.Count > 1 ||
                                e.GroupedVariables[0] != tail.GroupedVariables[0])
                                break;

                            tailIdx = cursor;
                        }

                        if (i == tailIdx)
                        {
                            MessageManager.ShowLine($"{ e.Index.ToString() }:\t{ e.Content.ToString() } (0x{ e.Content.GetValue().ToString("X8") })",level);
                            foreach (var v in e.GroupedVariables)
                            {
                                MessageManager.ShowLine($"   \t   *{ Assemble.AssemblyCode.GetBlockPathPrefix(v.DefinedBlock) + v.Name } (defined at { v.AssemblePosition.GenerateLocationText() })",level);
                            }
                        }
                        else
                        {
                            var tail = Readonlys[tailIdx];
                            MessageManager.Show($"{ e.Index.ToString() }-{tail.Index.ToString()}:\t{{ { e.Content.ToString() }",level);
                            for (int cursor = i + 1; cursor <= tailIdx; cursor++)
                            {
                                var cur = Readonlys[cursor];
                                MessageManager.Show($",{ cur.Content.ToString() }",level);
                            }
                            MessageManager.ShowLine(" }",level);
                            foreach (var v in e.GroupedVariables)
                            {
                                MessageManager.ShowLine($"   \t   *{ Assemble.AssemblyCode.GetBlockPathPrefix(v.DefinedBlock) + v.Name } (defined at { v.AssemblePosition.GenerateLocationText() })",level);
                            }
                        }

                        i = tailIdx;
                    }
                }

                {
                    MessageManager.ShowLine("",level);
                    MessageManager.ShowLine("Variables Mapping:",level);
                    for (int i = 0; i < Readwrites.Count; i++)
                    {
                        var e = Readwrites[i];
                        int tailIdx = i;
                        for (int cursor = i + 1; cursor < Readwrites.Count; cursor++)
                        {
                            var tail = Readwrites[cursor];
                            if (e.GroupedVariables.Count > 1 ||
                                tail.GroupedVariables.Count > 1 ||
                                e.GroupedVariables[0] != tail.GroupedVariables[0])
                                break;

                            tailIdx = cursor;
                        }

                        if (i == tailIdx)
                        {
                            MessageManager.Show($"{ e.Index.ToString() }:\t",level);
                            if (e.NeedInitialization)
                                MessageManager.ShowLine($"{ e.InitialValue.ToString() } (0x{ e.InitialValue.GetValue().ToString("X8") })",level);
                            else
                                MessageManager.ShowLine("Any",level);
                            foreach (var v in e.GroupedVariables)
                            {
                                MessageManager.ShowLine($"   \t   *{ Assemble.AssemblyCode.GetBlockPathPrefix(v.DefinedBlock) + v.Name } (defined at { v.AssemblePosition.GenerateLocationText() })",level);
                            }
                        }
                        else
                        {
                            var tail = Readwrites[tailIdx];
                            MessageManager.Show($"{ e.Index.ToString() }-{tail.Index.ToString()}:\t{{ { e.InitialValue.ToString() }",level);
                            for (int cursor = i + 1; cursor <= tailIdx; cursor++)
                            {
                                var cur = Readwrites[cursor];
                                MessageManager.Show($",{ cur.InitialValue.ToString() }",level);
                            }
                            MessageManager.ShowLine(" }",level);
                            foreach (var v in e.GroupedVariables)
                            {

                                MessageManager.ShowLine($"   \t   *{ Assemble.AssemblyCode.GetBlockPathPrefix(v.DefinedBlock) + v.Name } (defined at { v.AssemblePosition.GenerateLocationText() })",level);
                            }
                        }

                        i = tailIdx;
                    }
                }
            }
            MessageManager.ShowLine("",level);
        }
    }
}
