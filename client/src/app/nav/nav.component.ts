import { Component, OnInit } from '@angular/core';
import { AccountService } from '../_services/account.service';
import { Observable, of } from 'rxjs';
import { User } from '../_models/user';

@Component({
  selector: 'app-nav',
  templateUrl: './nav.component.html',
  styleUrls: ['./nav.component.css']
})
export class NavComponent implements OnInit {
  model: any = {};

  constructor(public accountServices: AccountService) { }

  ngOnInit(): void {
  }

  // getCurrentUser() {
  //   this.accountSevices.currentUser$.subscribe({
  //     next: user => this.isLoggedIn = !!user,
  //     error: error => console.log(error)
      
  //   })
  // }

  login() {
    this.accountServices.login(this.model).subscribe({
      next: respones => {
        console.log(respones)
      },
      error: error => {
        console.log(error)
      }     
    })
  }

  logout() {
    this.accountServices.logout();
  }

}
