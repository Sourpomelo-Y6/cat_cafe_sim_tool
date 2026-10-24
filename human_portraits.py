"""人物専用の採用画像。猫manifest・本文・ゲーム保存とは独立。"""
import hashlib
import json
import uuid
from pathlib import Path
from PIL import Image
from interaction_subjects import SubjectRegistry, identifier

class HumanPortraitStore:
    def __init__(self, directory):
        self.directory=Path(directory)
        self.path=self.directory/'human_portraits.json'
        self.subjects=SubjectRegistry(self.directory/'interaction_subjects.json')

    def read(self):
        data=json.loads(self.path.read_text(encoding='utf-8')) if self.path.exists() else {'version':1,'people':[]}
        self.validate(data)
        return data

    @staticmethod
    def validate(data):
        if not isinstance(data,dict) or set(data)!={'version','people'} or data['version']!=1 or not isinstance(data['people'],list):
            raise ValueError('人物画像台帳の形式が不正です。')
        ids=set();games=set()
        for r in data['people']:
            if set(r)!={'subject_id','game_id','image'} or not identifier(r['subject_id']) or not identifier(r['game_id']) or r['game_id']=='player' or r['subject_id'] in ids or r['game_id'] in games:
                raise ValueError('人物IDと客IDは一対一で指定してください。')
            ids.add(r['subject_id']);games.add(r['game_id'])
            import re
            if not re.fullmatch(r'people/[0-9a-f]{64}\.png',r['image']):raise ValueError('人物画像のパスが不正です。')

    def register(self, subject_id, source):
        row=self.subjects.get(subject_id)
        if not row or row['kind']!='speech' or not row['enabled'] or not row['game_id'] or row['game_id']=='player':
            raise ValueError('保存済みの有効な人物とゲーム客IDを指定してください。')
        source=Path(source)
        with Image.open(source) as im:
            if im.format!='PNG' or min(im.size)<32 or max(im.size)>2048:raise ValueError('32〜2048pxのPNGを選んでください。')
            im.verify()
        blob=source.read_bytes();relative='people/'+hashlib.sha256(blob).hexdigest()+'.png'
        data=self.read();data['people']=[r for r in data['people'] if r['subject_id']!=subject_id]
        data['people'].append(dict(subject_id=subject_id,game_id=row['game_id'],image=relative));self.validate(data)
        target=self.directory/relative;target.parent.mkdir(parents=True,exist_ok=True)
        if not target.exists():target.write_bytes(blob)
        temp=self.path.with_suffix('.tmp');temp.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');temp.replace(self.path)

    def remove(self, subject_id):
        data=self.read();data['people']=[r for r in data['people'] if r['subject_id']!=subject_id]
        self.path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

    def export_data(self):
        data=self.read();people=[];blobs={};warnings=[]
        for r in data['people']:
            subject=self.subjects.get(r['subject_id'])
            if not subject or subject['kind']!='speech' or not subject['enabled'] or subject['game_id']!=r['game_id']:
                warnings.append(r['subject_id']+'：人物対応が変更されたため画像を除外');continue
            path=self.directory/r['image']
            try:
                blob=path.read_bytes()
                if hashlib.sha256(blob).hexdigest()!=path.stem:raise ValueError('画像hash不一致')
                with Image.open(path) as im:im.verify()
            except (OSError,ValueError) as exc:
                warnings.append(r['subject_id']+'：画像欠損・破損のため共通記号を使用');continue
            people.append(r.copy());blobs[r['image']]=blob
        return {'version':1,'people':people},blobs,warnings

    def export(self,destination):
        destination=Path(destination).resolve()
        source=self.directory.resolve()
        if destination==source or source in destination.parents:raise ValueError('書き出し先は登録素材とは別にしてください。')
        data,blobs,warnings=self.export_data()
        # Empty pack intentionally clears adopted portraits; deterministic identity avoids repeated copies.
        identity=hashlib.sha256(json.dumps(data,sort_keys=True).encode()).hexdigest()[:32]
        data['pack_id']=identity
        folder=Path(destination)/('CatCafePeople-'+identity)
        folder.mkdir(parents=True,exist_ok=True)
        (folder/'people').mkdir(exist_ok=True)
        for relative,blob in blobs.items():
            target=folder/relative
            if not target.exists():target.write_bytes(blob)
        (folder/'human_portraits.json').write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
        return folder,warnings
