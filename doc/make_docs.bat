pandoc -o readme.md Blotch3D.docx
copy readme.md ..
doxygen Blotch3D.doxy
cd html
hhc index.hhp
copy index.chm ..\Blotch3D.chm
pause